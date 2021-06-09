using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Object = UnityEngine.Object;
using PlanBuild.Plans;
using UnityEngine.Rendering;
using Logger = Jotunn.Logger;

namespace PlanBuild.Blueprints
{
    public class Blueprint
    {
        public const string BlueprintPrefabName = "piece_blueprint";
        private const string HeaderName = "#Name: ";
        private const string HeaderDescription = "#Description";
        private const string HeaderSnapPoints = "#SnapPoints";
        private const string HeaderPieces = "#Pieces";
        public const string PlaceColliderName = "place_collider";
        public static bool logLoading = true;

        /// <summary>
        ///     File location of this blueprint instance.
        /// </summary>
        internal string m_fileLocation;

        /// <summary>
        ///     Name of the blueprint instance. Translates to &lt;m_name&gt;.blueprint in the filesystem
        /// </summary>
        public string m_name;

        /// <summary>
        ///     Array of the pieces this blueprint is made of
        /// </summary>
        public PieceEntry[] m_pieceEntries;

        /// <summary>
        ///     Array of the snappoints of this blueprint
        /// </summary>
        public Vector3[] m_snapPoints;

        /// <summary>
        ///     Dynamically generated prefab for this blueprint
        /// </summary>
        private GameObject m_prefab;

        /// <summary>
        ///     Name of the generated prefab of the blueprint instance. Is always "piece_blueprint (&lt;m_name&gt;)"
        /// </summary>
        private string m_prefabname;
        public string m_description = "";
        public Piece m_piece;

        /// <summary>
        ///     New "empty" Blueprint with a name but without any pieces. Call Capture() or Load() to add pieces to the blueprint.
        /// </summary>
        /// <param name="name"></param>
        private Blueprint()
        {

        }

        public static Blueprint FromWorld(string name)
        {
            return new Blueprint
            {
                m_fileLocation = Path.Combine(BlueprintManager.blueprintSaveDirectoryConfig.Value, name + ".blueprint"),
                m_name = name,
                m_prefabname = $"{BlueprintPrefabName} ({name})"
            };
        }

        public static Blueprint FromFile(string fileLocation)
        {
            string name = Path.GetFileNameWithoutExtension(fileLocation);
            return new Blueprint
            {
                m_fileLocation = fileLocation,
                m_name = name,
                m_prefabname = $"{BlueprintPrefabName} ({name})"
            };
        }

        /// <summary>
        ///     Number of pieces currently stored in this blueprint
        /// </summary>
        /// <returns></returns>
        public int GetPieceCount()
        {
            return m_pieceEntries.Count();
        }

        public Vector2 GetExtent()
        {
            return new Vector2(m_pieceEntries.Max(x => x.posX), m_pieceEntries.Max(x => x.posZ));
        }

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
                if (!CanCapture(piece))
                {
#if DEBUG
                    Jotunn.Logger.LogWarning("Ignoring piece " + piece + ", not able to make Plan");
#endif
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

            if (m_pieceEntries == null)
            {
                m_pieceEntries = new PieceEntry[pieces.Count()];
            }
            else if (m_pieceEntries.Length > 0)
            {
                Array.Clear(m_pieceEntries, 0, m_pieceEntries.Length - 1);
                Array.Resize(ref m_pieceEntries, pieces.Count());
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
                m_pieceEntries[i++] = new PieceEntry(pieceName, piece.m_category.ToString(), pos, quat, additionalInfo);
            }

            if (m_snapPoints == null)
            {
                m_snapPoints = new Vector3[snapPoints.Count()];
            }
            for (int j = 0; j < snapPoints.Count(); j++)
            {
                m_snapPoints[j] = snapPoints[j] - center;
            }

            return true;
        }

        public static bool CanCapture(Piece piece)
        {
            if (piece.name.StartsWith(BlueprintRunePrefab.BlueprintSnapPointName) || piece.name.StartsWith(BlueprintRunePrefab.BlueprintCenterPointName))
            {
                return true;
            }
            return piece.GetComponent<PlanPiece>() != null || PlanBuildPlugin.CanCreatePlan(piece);
        }

        // Scale down a Texture2D
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

        // Save thumbnail
        public void RecordFrame()
        {
            // Get a screenshot
            var screenShot = ScreenCapture.CaptureScreenshotAsTexture();

            // Calculate proper height
            var height = (int)Math.Round(160f * screenShot.height / screenShot.width);

            // Create thumbnail image from screenShot
            Texture2D thumbnail = ScaleTexture(screenShot, 160, height);

            // Save to file
            File.WriteAllBytes(Path.Combine(BlueprintManager.blueprintSaveDirectoryConfig.Value, m_name + ".png"), thumbnail.EncodeToPNG());

            // Destroy properly
            Object.Destroy(screenShot);
            Object.Destroy(thumbnail);
        }

        public bool Save()
        {
            if (m_pieceEntries == null)
            {
                Logger.LogWarning("No pieces stored to save");
                return false;
            }
            else
            {
                using (TextWriter tw = new StreamWriter(m_fileLocation))
                {
                    // tw.WriteLine(HeaderName + m_name);
                    // tw.WriteLine(HeaderDescription);
                    // tw.WriteLine(m_description);
                    if (m_snapPoints.Count() > 0)
                    {
                        tw.WriteLine(HeaderSnapPoints);
                        foreach (Vector3 pos in m_snapPoints)
                        {
                            tw.WriteLine(string.Join(";", PieceEntry.InvariantString(pos.x), PieceEntry.InvariantString(pos.y), PieceEntry.InvariantString(pos.z)));
                        }
                    }
                    tw.WriteLine(HeaderPieces);
                    foreach (var piece in m_pieceEntries)
                    {
                        tw.WriteLine(piece.line);
                    }

                    Logger.LogDebug("Wrote " + m_pieceEntries.Length + " pieces to " + m_fileLocation);
                }
            }

            return true;
        }

        private enum ParserState
        {
            Description,
            SnapPoints,
            Pieces
        }

        public bool Load()
        {
            try
            {
                string extension = Path.GetExtension(m_fileLocation).ToLowerInvariant();

                var lines = File.ReadAllLines(m_fileLocation).ToList();
                if (logLoading)
                {
                    Logger.LogDebug("read " + lines.Count + " pieces from " + m_fileLocation);
                }

                List<PieceEntry> pieceEntries = new List<PieceEntry>();
                List<Vector3> snapPoints = new List<Vector3>();

                ParserState state = ParserState.Pieces;

                foreach (var line in lines)
                {
                    if (line.StartsWith(HeaderName))
                    {
                        m_name = line.Substring(HeaderName.Length);
                        continue;
                    }
                    if (line == HeaderDescription)
                    {
                        state = ParserState.Description;
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
                        case ParserState.Description:
                            if (m_description.Length != 0)
                            {
                                m_description += "\n";
                            }
                            m_description += line;
                            continue;
                        case ParserState.SnapPoints:
                            snapPoints.Add(ParsePosition(line));
                            continue;
                        case ParserState.Pieces:
                            PieceEntry pieceEntry;
                            switch (extension)
                            {
                                case ".vbuild":
                                    pieceEntry = PieceEntry.FromVBuild(line);
                                    break;
                                case ".blueprint":
                                    pieceEntry = PieceEntry.FromBlueprint(line);
                                    break;
                                default:
                                    if (logLoading)
                                    {
                                        Logger.LogWarning("Unknown extension " + extension);
                                    }
                                    return false;
                            }
                            pieceEntries.Add(pieceEntry);
                            continue;
                    }
                }

                m_pieceEntries = pieceEntries.ToArray();
                m_snapPoints = snapPoints.ToArray();

                return true;
            }
            catch (Exception e)
            {
                if (logLoading)
                {
                    Logger.LogError(e);
                }
                else
                {
                    throw e;
                }
                return false;
            }
        }

        private Vector3 ParsePosition(string line)
        {
            string[] parts = line.Split(';');
            return new Vector3(PieceEntry.InvariantFloat(parts[0]), PieceEntry.InvariantFloat(parts[1]), PieceEntry.InvariantFloat(parts[2]));
        }

        public void CalculateCost()
        {
            if (m_pieceEntries == null)
            {
                Logger.LogWarning("No pieces loaded");
                return;
            }


        }

        public bool CreatePrefab()
        {
            if (m_prefab != null)
            {
                return false;
            }
            Logger.LogDebug($"Creating dynamic prefab {m_prefabname}");

            if (m_pieceEntries == null)
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
            m_prefab = Object.Instantiate(stub);
            ZNetView.m_forceDisableInit = false;
            m_prefab.name = m_prefabname;

            m_piece = m_prefab.GetComponent<Piece>();

            if (File.Exists(Path.Combine(BlueprintManager.blueprintSaveDirectoryConfig.Value, m_name + ".png")))
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(Path.Combine(BlueprintManager.blueprintSaveDirectoryConfig.Value, m_name + ".png")));

                m_piece.m_icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            }

            m_piece.m_name = m_name;
            m_piece.m_enabled = true;

            // Instantiate child objects
            if (!GhostInstantiate(m_prefab))
            {
                Logger.LogWarning("Could not create prefab");
                Object.DestroyImmediate(m_prefab);
                return false;
            }

            // Add to known prefabs
            CustomPiece CP = new CustomPiece(m_prefab, new PieceConfig
            {
                PieceTable = BlueprintRunePrefab.PieceTableName,
                Category = BlueprintRunePrefab.CategoryBlueprints
            });
            CP.Piece.m_description += "\nFile location: " + Path.GetFullPath(m_fileLocation);
            PieceManager.Instance.AddPiece(CP);
            //PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName).m_pieces.Add(m_prefab);
            AddToPieceTable();
            PrefabManager.Instance.RegisterToZNetScene(m_prefab);

            return true;
        }

        public void AddToPieceTable()
        {
            if (m_prefab == null)
            {
                return;
            }

            var table = PieceManager.Instance.GetPieceTable(BlueprintRunePrefab.PieceTableName);
            if (table == null)
            {
                Logger.LogWarning($"{BlueprintRunePrefab.PieceTableName} not found");
                return;
            }

            if (!table.m_pieces.Contains(m_prefab))
            {
                Logger.LogDebug($"Adding {m_prefabname} to {BlueprintRunePrefab.BlueprintRuneName}"); 
                table.m_pieces.Add(m_prefab);
            }
        }

        public void Destroy()
        {
            if (m_prefab == null)
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

            if (table.m_pieces.Contains(m_prefab))
            {
                Logger.LogInfo($"Removing {m_prefabname} from {BlueprintRunePrefab.BlueprintRuneName}");

                table.m_pieces.Remove(m_prefab);
            }

            // Remove from prefabs
            PieceManager.Instance.RemovePiece(m_prefabname);
            PrefabManager.Instance.DestroyPrefab(m_prefabname);
        }

        private bool GhostInstantiate(GameObject baseObject)
        {
            var ret = true;
            ZNetView.m_forceDisableInit = true;

            try
            {
                var pieces = new List<PieceEntry>(m_pieceEntries);
                var maxX = pieces.Max(x => x.posX);
                var maxZ = pieces.Max(x => x.posZ);

                foreach (Vector3 snapPoint in m_snapPoints)
                {
                    GameObject snapPointObject = new GameObject
                    {
                        name = "_snappoint",
                        layer = LayerMask.NameToLayer("piece"),
                        tag = "snappoint"
                    };
                    snapPointObject.SetActive(false);
                    Object.Instantiate(snapPointObject, snapPoint, Quaternion.identity, baseObject.transform);
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
                        Logger.LogWarning("No prefab found for " + piece.name + "! You are probably missing a dependency for blueprint " + m_name);
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
                Logger.LogError($"Error while instantiating {m_name}: {ex}");
                ret = false;
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }

            return ret;
        }

        private static void MakeGhost(GameObject child)
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
                Piece = m_prefabname,
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
                Piece = m_prefabname
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
                return newbp.m_name;
            }

            public void SetText(string text)
            {
                newbp.m_name = text;
                newbp.m_prefabname = $"{BlueprintPrefabName} ({newbp.m_name})";
                newbp.m_fileLocation = Path.Combine(BlueprintManager.blueprintSaveDirectoryConfig.Value, newbp.m_name + ".blueprint");
                if (newbp.Save())
                {
                    if (BlueprintManager.Instance.m_blueprints.ContainsKey(newbp.m_name))
                    {
                        Blueprint oldbp = BlueprintManager.Instance.m_blueprints[newbp.m_name];
                        oldbp.Destroy();
                        oldbp.RemoveKeyHint();
                        BlueprintManager.Instance.m_blueprints.Remove(newbp.m_name);
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
                BlueprintManager.Instance.m_blueprints.Add(newbp.m_name, newbp);

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