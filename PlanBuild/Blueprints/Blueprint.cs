using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using PlanBuild.Plans;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;

namespace PlanBuild.Blueprints
{
    internal class Blueprint
    {
        public const string BlueprintPrefabName = "piece_blueprint";
        public const string PlaceColliderName = "place_collider";

        private const string HeaderName = "#Name:";
        private const string HeaderCreator = "#Creator:";
        private const string HeaderDescription = "#Description:";
        private const string HeaderSnapPoints = "#SnapPoints";
        private const string HeaderPieces = "#Pieces";

        public enum Format
        {
            VBuild,
            Blueprint
        }

        private enum ParserState
        {
            SnapPoints,
            Pieces
        }

        /// <summary>
        ///     File location of this blueprint instance.
        /// </summary>
        public string FileLocation;

        /// <summary>
        ///     ID of the blueprint instance.
        /// </summary>
        public string ID;

        /// <summary>
        ///     Name of the blueprint instance.
        /// </summary>
        public string Name;

        /// <summary>
        ///     Name of the player who created this blueprint.
        /// </summary>
        public string Creator;

        /// <summary>
        ///     Optional description for this blueprint
        /// </summary>
        public string Description = string.Empty;

        /// <summary>
        ///     Array of the pieces this blueprint is made of
        /// </summary>
        public PieceEntry[] PieceEntries;

        /// <summary>
        ///     Array of the snappoints of this blueprint
        /// </summary>
        public SnapPoint[] SnapPoints;

        /// <summary>
        ///     Dynamically generated prefab for this blueprint
        /// </summary>
        private GameObject Prefab;

        /// <summary>
        ///     Name of the generated prefab of the blueprint instance. Is always "piece_blueprint (&lt;Name&gt;)"
        /// </summary>
        private string PrefabName;

        /// <summary>
        ///     Create a blueprint instance from a file in the filesystem. Reads VBuild and Blueprint files.
        /// </summary>
        /// <param name="fileLocation">Absolute path to the blueprint file</param>
        /// <returns><see cref="Blueprint"/> instance, ID equals file name</returns>
        public static Blueprint FromPath(string fileLocation)
        {
            string name = Path.GetFileNameWithoutExtension(fileLocation);
            string extension = Path.GetExtension(fileLocation).ToLowerInvariant();
            
            Format format;
            switch (extension)
            {
                case ".vbuild":
                    format = Format.VBuild;
                    break;
                case ".blueprint":
                    format = Format.Blueprint;
                    break;
                default:
                    throw new Exception($"Format {extension} not recognized");
            }

            string[] lines = File.ReadAllLines(fileLocation);

            Blueprint ret = FromArray(name, lines, format);
            ret.FileLocation = fileLocation;
            if (string.IsNullOrEmpty(ret.Name))
            {
                ret.Name = name;
                ret.PrefabName = $"{BlueprintPrefabName} ({name})";
            }

            return ret;
        }

        /// <summary>
        ///     Create a blueprint instance with a given ID from a BLOB.
        /// </summary>
        /// <param name="id">The unique blueprint ID</param>
        /// <param name="payload">BLOB with blurprint data</param>
        /// <returns></returns>
        public static Blueprint FromBlob(string id, byte[] payload)
        {
            List<string> lines = new List<string>();
            using (MemoryStream m = new MemoryStream(payload))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    lines.Add(reader.ReadString());
                    reader.ReadChar();
                }
            }

            Blueprint ret = FromArray(id, lines.ToArray(), Format.Blueprint);
            return ret;
        }

        /// <summary>
        ///     Create a blueprint instance with a given ID from a string array holding blueprint information.
        /// </summary>
        /// <param name="id">The unique blueprint ID</param>
        /// <param name="lines">String array with either VBuild or Blueprint format information</param>
        /// <param name="format"><see cref="Format"/> of the blueprint lines</param>
        /// <returns></returns>
        public static Blueprint FromArray(string id, string[] lines, Format format)
        {
            Blueprint ret = new Blueprint();
            ret.ID = id;

            List<PieceEntry> pieceEntries = new List<PieceEntry>();
            List<SnapPoint> snapPoints = new List<SnapPoint>();

            ParserState state = ParserState.Pieces;

            foreach (var line in lines)
            {
                if (line.StartsWith(HeaderName))
                {
                    ret.Name = line.Substring(HeaderName.Length);
                    ret.PrefabName = $"{BlueprintPrefabName} ({ret.Name})";
                    continue;
                }
                if (line.StartsWith(HeaderCreator))
                {
                    ret.Creator = line.Substring(HeaderCreator.Length);
                    continue;
                }
                if (line.StartsWith(HeaderDescription))
                {
                    ret.Description = line.Substring(HeaderDescription.Length);
                    continue;
                }
                if (line == HeaderSnapPoints)
                {
                    state = ParserState.SnapPoints;
                    continue;
                }
                if (line == HeaderPieces)
                {
                    state = ParserState.Pieces;
                    continue;
                }
                switch (state)
                {
                    case ParserState.SnapPoints:
                        snapPoints.Add(new SnapPoint(line));
                        continue;
                    case ParserState.Pieces:
                        switch (format)
                        {
                            case Format.VBuild:
                                pieceEntries.Add(PieceEntry.FromVBuild(line));
                                break;

                            case Format.Blueprint:
                                pieceEntries.Add(PieceEntry.FromBlueprint(line));
                                break;
                        }
                        continue;
                }
            }

            ret.PieceEntries = pieceEntries.ToArray();
            ret.SnapPoints = snapPoints.ToArray();

            return ret;
        }

        /// <summary>
        ///     Creates a string array of this blueprint instance in Blueprint <see cref="Format"/>.
        /// </summary>
        /// <returns></returns>
        public string[] ToArray()
        {
            if (PieceEntries == null)
            {
                return null;
            }
            else
            {
                List<string> ret = new List<string>();

                ret.Add(HeaderName + Name);
                ret.Add(HeaderCreator + Creator);
                ret.Add(HeaderDescription + Description);
                if (SnapPoints.Count() > 0)
                {
                    ret.Add(HeaderSnapPoints);
                    foreach (SnapPoint snapPoint in SnapPoints)
                    {
                        ret.Add(snapPoint.line);
                    }
                }
                ret.Add(HeaderPieces);
                foreach (var piece in PieceEntries)
                {
                    ret.Add(piece.line);
                }

                return ret.ToArray();
            }
        }

        /// <summary>
        ///     Creates a BLOB of this blueprint instance in Blueprint <see cref="Format"/>.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBlob()
        {
            string[] lines = ToArray();
            if (lines == null)
            {
                return null;
            }

            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    foreach (string line in lines)
                    {
                        writer.Write(line);
                        writer.Write('\r');
                    }
                }
                return m.ToArray();
            }
        }

        /// <summary>
        ///     Number of pieces currently stored in this blueprint
        /// </summary>
        /// <returns></returns>
        public int GetPieceCount()
        {
            return PieceEntries.Count();
        }

        /// <summary>
        ///     Maximum X and Z position of this blueprint
        /// </summary>
        /// <returns></returns>
        public Vector2 GetExtent()
        {
            return new Vector2(PieceEntries.Max(x => x.posX), PieceEntries.Max(x => x.posZ));
        }

        /// <summary>
        ///     Capture all pieces within the radius at a certain position
        /// </summary>
        /// <param name="position">Center position of the capture</param>
        /// <param name="radius">Capture radius</param>
        /// <returns></returns>
        public bool Capture(Vector3 position, float radius)
        {
            Logger.LogDebug("Collecting piece information");

            var numPieces = 0;
            var collected = new List<Piece>();
            var snapPoints = new List<Vector3>();
            Transform centerPiece = null;

            foreach (var piece in BlueprintManager.GetPiecesInRadius(position, radius))
            {
                if (piece.name.StartsWith(BlueprintRunePrefab.BlueprintSnapPointName))
                {
                    snapPoints.Add(piece.transform.position);
                    WearNTear wearNTear = piece.GetComponent<WearNTear>();
                    wearNTear.Remove();
                    continue;
                }
                if (piece.name.StartsWith(BlueprintRunePrefab.BlueprintCenterPointName))
                {
                    if (centerPiece == null)
                    {
                        centerPiece = piece.transform;
                    }
                    else
                    {
                        Logger.LogWarning("Multiple center points! Ignoring @ " + piece.transform.position);
                    }
                    WearNTear wearNTear = piece.GetComponent<WearNTear>();
                    wearNTear.Remove();
                    continue;
                }
                if (!BlueprintManager.CanCapture(piece))
                {
                    Logger.LogWarning("Ignoring piece " + piece + ", not able to make Plan");
                    continue;
                }
                piece.GetComponent<WearNTear>()?.Highlight();
                collected.Add(piece);
                numPieces++;
            }

            if (collected.Count() == 0)
            {
                return false;
            }

            Logger.LogDebug($"Found {numPieces} in a radius of {radius:F2}");

            Vector3 center;
            if (centerPiece == null)
            {
                // Relocate Z
                var minZ = 9999999.9f;
                var minX = 9999999.9f;
                var minY = 9999999.9f;

                foreach (var piece in collected)
                {
                    minX = Math.Min(piece.m_nview.GetZDO().m_position.x, minX);
                    minZ = Math.Min(piece.m_nview.GetZDO().m_position.z, minZ);
                    minY = Math.Min(piece.m_nview.GetZDO().m_position.y, minY);
                }

                Logger.LogDebug($"{minX} - {minY} - {minZ}");

                center = new Vector3(minX, minY, minZ);
            }
            else
            {
                center = centerPiece.position;
            }

            // select and order instance piece entries
            var pieces = collected
                    .OrderBy(x => x.transform.position.y)
                    .ThenBy(x => x.transform.position.x)
                    .ThenBy(x => x.transform.position.z);

            // create instance piece entries
            if (PieceEntries == null)
            {
                PieceEntries = new PieceEntry[pieces.Count()];
            }
            else if (PieceEntries.Length > 0)
            {
                Array.Clear(PieceEntries, 0, PieceEntries.Length - 1);
                Array.Resize(ref PieceEntries, pieces.Count());
            }

            uint i = 0;
            foreach (var piece in pieces)
            {
                var pos = piece.m_nview.GetZDO().m_position - center;

                var quat = piece.m_nview.GetZDO().m_rotation;
                quat.eulerAngles = piece.transform.eulerAngles;

                var additionalInfo = piece.GetComponent<TextReceiver>() != null ? piece.GetComponent<TextReceiver>().GetText() : "";

                string pieceName = piece.name.Split('(')[0];
                if (pieceName.EndsWith(PlanPiecePrefab.PlannedSuffix))
                {
                    pieceName = pieceName.Replace(PlanPiecePrefab.PlannedSuffix, null);
                }
                PieceEntries[i++] = new PieceEntry(pieceName, piece.m_category.ToString(), pos, quat, additionalInfo);
            }

            // create instance snap points
            if (SnapPoints == null)
            {
                SnapPoints = new SnapPoint[snapPoints.Count()];
            }
            else if (SnapPoints.Length > 0)
            {
                Array.Clear(SnapPoints, 0, SnapPoints.Length - 1);
                Array.Resize(ref SnapPoints, snapPoints.Count());
            }

            for (int j = 0; j < snapPoints.Count(); j++)
            {
                SnapPoints[j] = new SnapPoint(snapPoints[j] - center);
            }

            return true;
        }

        /// <summary>
        ///     Scale down a Texture2D
        /// </summary>
        /// <param name="orig">Original texture</param>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        /// <returns></returns>
        public Texture2D ScaleTexture(Texture2D orig, int width, int height)
        {
            var result = new Texture2D(width, height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var xp = 1f * x / width;
                    var yp = 1f * y / height;
                    var xo = (int)Mathf.Round(xp * orig.width); //Other X pos
                    var yo = (int)Mathf.Round(yp * orig.height); //Other Y pos
                    Color origPixel = orig.GetPixel(xo, yo);
                    origPixel.a = 1f;
                    result.SetPixel(x, y, origPixel);
                }
            }

            result.Apply();
            return result;
        }

        /// <summary>
        ///     Save thumbnail
        /// </summary>
        public void RecordFrame()
        {
            // Get a screenshot
            var screenShot = ScreenCapture.CaptureScreenshotAsTexture();

            // Calculate proper height
            var height = (int)Math.Round(160f * screenShot.height / screenShot.width);

            // Create thumbnail image from screenShot
            Texture2D thumbnail = ScaleTexture(screenShot, 160, height);

            // Save to file
            File.WriteAllBytes(Path.Combine(BlueprintConfig.blueprintSaveDirectoryConfig.Value, ID + ".png"), thumbnail.EncodeToPNG());

            // Destroy properly
            Object.Destroy(screenShot);
            Object.Destroy(thumbnail);
        }

        /// <summary>
        ///     Save this instance as a blueprint file to <see cref="FileLocation"/>
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            if (PieceEntries == null)
            {
                Logger.LogWarning("No pieces stored to save");
                return false;
            }
            else
            {
                using (TextWriter tw = new StreamWriter(FileLocation))
                {
                    foreach (string line in ToArray())
                    {
                        tw.WriteLine(line);
                    }

                    Logger.LogDebug("Wrote " + PieceEntries.Length + " pieces to " + FileLocation);
                }
            }

            return true;
        }

        public void CalculateCost()
        {
            if (PieceEntries == null)
            {
                Logger.LogWarning("No pieces loaded");
                return;
            }
        }

        public bool CreatePrefab()
        {
            if (Prefab != null)
            {
                return false;
            }
            Logger.LogDebug($"Creating dynamic prefab {PrefabName}");

            if (PieceEntries == null)
            {
                Logger.LogWarning("No pieces loaded");
                return false;
            }

            // Get Stub from PrefabManager
            var stub = PrefabManager.Instance.GetPrefab(BlueprintPrefabName);
            if (stub == null)
            {
                Logger.LogWarning("Could not load blueprint stub from prefabs");
                return false;
            }

            // Instantiate clone from stub
            ZNetView.m_forceDisableInit = true;
            Prefab = Object.Instantiate(stub);
            ZNetView.m_forceDisableInit = false;
            Prefab.name = PrefabName;

            Piece piece = Prefab.GetComponent<Piece>();

            if (File.Exists(Path.Combine(BlueprintConfig.blueprintSaveDirectoryConfig.Value, ID + ".png")))
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(Path.Combine(BlueprintConfig.blueprintSaveDirectoryConfig.Value, ID + ".png")));
                piece.m_icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            }

            piece.m_name = Name;
            piece.m_enabled = true;

            // Instantiate child objects
            if (!GhostInstantiate(Prefab))
            {
                Logger.LogWarning("Could not create prefab");
                Object.DestroyImmediate(Prefab);
                return false;
            }

            // Add to known pieces
            CustomPiece CP = new CustomPiece(Prefab, new PieceConfig
            {
                PieceTable = BlueprintRunePrefab.PieceTableName,
                Category = BlueprintRunePrefab.CategoryBlueprints
            });
            CP.Piece.m_description += "\nFile name: " + Path.GetFileName(FileLocation);
            if (!string.IsNullOrEmpty(Creator))
            {
                CP.Piece.m_description += "\nCreator: " + Creator;
            }
            if (!string.IsNullOrEmpty(Description))
            {
                CP.Piece.m_description += "\nDescription: " + Description;
            }
            PieceManager.Instance.AddPiece(CP);
            //PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName).m_pieces.Add(m_prefab);
            AddToPieceTable();
            PrefabManager.Instance.RegisterToZNetScene(Prefab);

            return true;
        }

        public void AddToPieceTable()
        {
            if (Prefab == null)
            {
                return;
            }

            var table = PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName);
            if (table == null)
            {
                Logger.LogWarning($"{BlueprintRunePrefab.PieceTableName} not found");
                return;
            }

            if (!table.m_pieces.Contains(Prefab))
            {
                Logger.LogDebug($"Adding {PrefabName} to {BlueprintRunePrefab.BlueprintRuneName}"); 
                table.m_pieces.Add(Prefab);
            }
        }

        public void Destroy()
        {
            if (Prefab == null)
            {
                return;
            }

            // Remove from PieceTable
            var table = PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName);
            if (table == null)
            {
                Logger.LogWarning($"{BlueprintRunePrefab.PieceTableName} not found");
                return;
            }

            if (table.m_pieces.Contains(Prefab))
            {
                Logger.LogInfo($"Removing {PrefabName} from {BlueprintRunePrefab.BlueprintRuneName}");

                table.m_pieces.Remove(Prefab);
            }

            // Remove from prefabs
            PieceManager.Instance.RemovePiece(PrefabName);
            PrefabManager.Instance.DestroyPrefab(PrefabName);
        }

        /// <summary>
        ///     Instantiate this blueprints placement ghost
        /// </summary>
        /// <param name="baseObject"></param>
        /// <returns></returns>
        private bool GhostInstantiate(GameObject baseObject)
        {
            var ret = true;
            ZNetView.m_forceDisableInit = true;

            try
            {
                var pieces = new List<PieceEntry>(PieceEntries);
                var maxX = pieces.Max(x => x.posX);
                var maxZ = pieces.Max(x => x.posZ);

                foreach (SnapPoint snapPoint in SnapPoints)
                {
                    GameObject snapPointObject = new GameObject
                    {
                        name = "_snappoint",
                        layer = LayerMask.NameToLayer("piece"),
                        tag = "snappoint"
                    };
                    snapPointObject.SetActive(false);
                    Object.Instantiate(snapPointObject, snapPoint.GetPosition(), Quaternion.identity, baseObject.transform);
                }

                //Tiny collider for accurate placement
                GameObject gameObject = new GameObject(PlaceColliderName);
                gameObject.transform.SetParent(baseObject.transform);
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.002f;

                var tf = baseObject.transform;
                var quat = Quaternion.Euler(0, tf.rotation.eulerAngles.y, 0);
                tf.SetPositionAndRotation(tf.position, quat);
                tf.position -= tf.right * (maxX / 2f);
                tf.position += tf.forward * 5f;

                var prefabs = new Dictionary<string, GameObject>();
                foreach (var piece in pieces.GroupBy(x => x.name).Select(x => x.FirstOrDefault()))
                {
                    var go = PrefabManager.Instance.GetPrefab(piece.name);
                    if (!go)
                    {
                        Logger.LogWarning("No prefab found for " + piece.name + "! You are probably missing a dependency for blueprint " + Name);
                    }
                    else
                    {
                        go.transform.SetPositionAndRotation(go.transform.position, quat);
                        prefabs.Add(piece.name, go);
                    }
                }

                for (int i = 0; i < pieces.Count; i++)
                {
                    PieceEntry piece = pieces[i];
                    var pos = tf.position + piece.GetPosition();

                    var q = piece.GetRotation();

                    GameObject pieceObject = new GameObject("piece_entry (" + i + ")");
                    pieceObject.transform.SetParent(tf);
                    pieceObject.transform.rotation = q;
                    pieceObject.transform.position = pos;

                    if (prefabs.TryGetValue(piece.name, out var prefab))
                    {
                        GameObject ghostPrefab;
                        Vector3 ghostPosition;
                        Quaternion ghostRotation;
                        if (prefab.TryGetComponent(out WearNTear wearNTear) && wearNTear.m_new)
                        {
                            //Only instantiate the visual part
                            ghostPrefab = wearNTear.m_new;
                            ghostRotation = ghostPrefab.transform.localRotation;
                            ghostPosition = ghostPrefab.transform.localPosition;
                        }
                        else
                        {
                            //No WearNTear?? Just use the entire prefab
                            ghostPrefab = prefab;
                            ghostRotation = Quaternion.identity;
                            ghostPosition = Vector3.zero;
                        }

                        var child = Object.Instantiate(ghostPrefab, pieceObject.transform);
                        child.transform.localRotation = ghostRotation;
                        child.transform.localPosition = ghostPosition;
                        MakeGhost(child);

                        //Doors have a dynamic object that also needs to be added
                        if (prefab.TryGetComponent(out Door door))
                        {
                            GameObject doorPrefab = door.m_doorObject;
                            var doorChild = Object.Instantiate(doorPrefab, pieceObject.transform);
                            doorChild.transform.localRotation = doorPrefab.transform.localRotation;
                            doorChild.transform.localPosition = doorPrefab.transform.localPosition;
                            MakeGhost(doorChild);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while instantiating {Name}: {ex}");
                ret = false;
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }

            return ret;
        }

        /// <summary>
        ///     Prepare a GameObject for the placement ghost
        /// </summary>
        /// <param name="child"></param>
        private void MakeGhost(GameObject child)
        {
            // A Ghost doesn't need fancy scripts
            foreach (var component in child.GetComponentsInChildren<MonoBehaviour>())
            {
                Object.Destroy(component);
            }

            //Disable ripple effect on ghost (only visible when using Skuld crystal)
            MeshRenderer[] meshRenderers = child.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer.sharedMaterial != null)
                {
                    Material[] sharedMaterials = meshRenderer.sharedMaterials;
                    for (int j = 0; j < sharedMaterials.Length; j++)
                    {
                        Material material = new Material(sharedMaterials[j]);
                        material.SetFloat("_RippleDistance", 0f);
                        material.SetFloat("_ValueNoise", 0f);
                        sharedMaterials[j] = material;
                    }
                    meshRenderer.sharedMaterials = sharedMaterials;
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            // m_placementGhost is updated on the fly instead
            // ShaderHelper.UpdateTextures(child, ShaderHelper.ShaderState.Floating);
        }

        internal void CreateKeyHint()
        {
            KeyHintConfig KHC = new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = PrefabName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig { Name = BlueprintManager.planSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpplace" },
                    new ButtonConfig { Name = "AltPlace", HintToken = "$hud_bpflatten" },
                    new ButtonConfig { Name = "Crouch", HintToken = "$hud_bpdirect" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate" }
                }
            };
            GUIManager.Instance.AddKeyHint(KHC);
        }

        internal void RemoveKeyHint()
        {
            KeyHintConfig KHC = new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = PrefabName
            };
            GUIManager.Instance.RemoveKeyHint(KHC);
        }

        /// <summary>
        ///     Helper class for naming and saving a captured blueprint via GUI
        ///     Implements the Interface <see cref="TextReceiver" />. SetText is called from <see cref="TextInput" /> upon entering
        ///     an name for the blueprint.<br />
        ///     Save the actual blueprint and add it to the list of known blueprints.
        /// </summary>
        internal class BlueprintSaveGUI : TextReceiver
        {
            private Blueprint newbp;

            public BlueprintSaveGUI(Blueprint bp)
            {
                newbp = bp;
            }

            public string GetText()
            {
                return newbp.Name;
            }

            public void SetText(string text)
            {
                newbp.ID = $"{Player.m_localPlayer.GetPlayerName()}_{text}";
                newbp.Name = text;
                newbp.PrefabName = $"{BlueprintPrefabName} ({newbp.Name})";
                newbp.Creator = Player.m_localPlayer.GetPlayerName();
                newbp.FileLocation = Path.Combine(BlueprintConfig.blueprintSaveDirectoryConfig.Value, newbp.ID + ".blueprint");
                if (newbp.Save())
                {
                    if (BlueprintManager.Instance.Blueprints.ContainsKey(newbp.ID))
                    {
                        Blueprint oldbp = BlueprintManager.Instance.Blueprints[newbp.ID];
                        oldbp.Destroy();
                        oldbp.RemoveKeyHint();
                        BlueprintManager.Instance.Blueprints.Remove(newbp.ID);
                    }

                    PlanBuildPlugin.Instance.StartCoroutine(AddBlueprint());
                }
            }

            public IEnumerator AddBlueprint()
            {
                bool oldHud = DisableHud();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                newbp.RecordFrame();

                Hud.instance.m_userHidden = oldHud;
                Hud.instance.SetVisible(true);
                Hud.instance.Update();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();

                newbp.CreatePrefab();

                newbp.AddToPieceTable();

                newbp.CreateKeyHint();

                Player.m_localPlayer.UpdateKnownRecipesList();
                Player.m_localPlayer.UpdateAvailablePiecesList();
                BlueprintManager.Instance.Blueprints.Add(newbp.Name, newbp);

                Logger.LogInfo("Blueprint created");

                newbp = null;
            }

            private bool DisableHud()
            {
                Console.instance.m_chatWindow.gameObject.SetActive(false);
                Console.instance.Update();
                bool oldHud = Hud.instance.m_userHidden;
                Hud.instance.m_userHidden = true;
                Hud.instance.SetVisible(false);
                Hud.instance.Update();

                return oldHud;
            }
        }
    }
}