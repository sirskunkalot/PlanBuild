using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
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
        public const string ZDOBlueprintName = "BlueprintName";
        public const string PieceBlueprintName = "piece_blueprint";
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
        ///     Indicates the format of this blueprints file in the filesystem.
        /// </summary>
        public Format FileFormat;

        /// <summary>
        ///     File location of this blueprints icon.
        /// </summary>
        public string IconLocation;

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
        ///     Array of the <see cref="PieceEntry"/>s this blueprint is made of
        /// </summary>
        public PieceEntry[] PieceEntries;

        /// <summary>
        ///     Array of the <see cref="SnapPoint"/>s of this blueprint
        /// </summary>
        public SnapPoint[] SnapPoints;

        /// <summary>
        ///     Thumbnail of this blueprint as a <see cref="Texture2D"/>
        /// </summary>
        public Texture2D Thumbnail;

        /// <summary>
        ///     Name of the generated prefab of the blueprint instance. Is always "piece_blueprint (&lt;ID&gt;)"
        /// </summary>
        private string PrefabName;

        /// <summary>
        ///     Dynamically generated prefab for this blueprint
        /// </summary>
        private GameObject Prefab;

        /// <summary>
        ///     Dynamically generated KeyHint for this blueprint
        /// </summary>
        private KeyHintConfig KeyHint;

        /// <summary>
        ///     Create a blueprint instance from a file in the filesystem. Reads VBuild and Blueprint files. 
        ///     Reads an optional thumbnail from a PNG file with the same name as the blueprint.
        /// </summary>
        /// <param name="fileLocation">Absolute path to the blueprint file</param>
        /// <returns><see cref="Blueprint"/> instance with an optional thumbnail, ID equals file name</returns>
        public static Blueprint FromFile(string fileLocation)
        {
            string filename = Path.GetFileNameWithoutExtension(fileLocation);
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
            Logger.LogDebug($"Read {lines.Length} lines from {fileLocation}");

            Blueprint ret = FromArray(filename, lines, format);
            ret.FileFormat = format;
            ret.FileLocation = fileLocation;
            ret.IconLocation = fileLocation.Replace(extension, ".png");
            
            if (File.Exists(ret.IconLocation))
            {
                ret.Thumbnail = AssetUtils.LoadTexture(ret.IconLocation, relativePath: false);
                Logger.LogDebug($"Read thumbnail data from {ret.IconLocation}");
            }

            return ret;
        }

        /// <summary>
        ///     Create a blueprint instance from a <see cref="ZPackage"/>.
        /// </summary>
        /// <param name="pkg"></param>
        /// <returns><see cref="Blueprint"/> instance with an optional thumbnail, ID comes from the <see cref="ZPackage"/></returns>
        public static Blueprint FromZPackage(ZPackage pkg)
        {
            string id = pkg.ReadString();
            Blueprint bp = FromBlob(id, pkg.ReadByteArray());
            return bp;
        }

        /// <summary>
        ///     Create a blueprint instance with a given ID from a BLOB.
        /// </summary>
        /// <param name="id">The unique blueprint ID</param>
        /// <param name="payload">BLOB with blueprint data</param>
        /// <returns><see cref="Blueprint"/> instance with an optional thumbnail</returns>
        public static Blueprint FromBlob(string id, byte[] payload)
        {
            Blueprint ret;
            List<string> lines = new List<string>();
            using (MemoryStream m = new MemoryStream(payload))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    int numLines = reader.ReadInt32();
                    for (int i = 0; i < numLines; i++)
                    {
                        lines.Add(reader.ReadString());
                    }
                    ret = FromArray(id, lines.ToArray(), Format.Blueprint);

                    int numBytes = reader.ReadInt32();
                    if (numBytes > 0)
                    {
                        byte[] thumbnailBytes = reader.ReadBytes(numBytes);
                        ret.Thumbnail = new Texture2D(1, 1);
                        ret.Thumbnail.LoadImage(thumbnailBytes);
                    }
                }
            }

            return ret;
        }

        /// <summary>
        ///     Create a blueprint instance with a given ID from a string array holding blueprint information.
        /// </summary>
        /// <param name="id">The unique blueprint ID</param>
        /// <param name="lines">String array with either VBuild or Blueprint format information</param>
        /// <param name="format"><see cref="Format"/> of the blueprint lines</param>
        /// <returns><see cref="Blueprint"/> instance built from the given lines without a thumbnail and the default filesystem paths</returns>
        public static Blueprint FromArray(string id, string[] lines, Format format)
        {
            Blueprint ret = new Blueprint();
            ret.ID = id;
            ret.PrefabName = $"{PieceBlueprintName}:{id}";
            ret.FileFormat = Format.Blueprint;
            ret.FileLocation = Path.Combine(BlueprintConfig.blueprintSaveDirectoryConfig.Value, $"{id}.blueprint");
            ret.IconLocation = Path.Combine(BlueprintConfig.blueprintSaveDirectoryConfig.Value, $"{id}.png");

            List<PieceEntry> pieceEntries = new List<PieceEntry>();
            List<SnapPoint> snapPoints = new List<SnapPoint>();

            ParserState state = ParserState.Pieces;

            foreach (var line in lines)
            {
                if (line.StartsWith(HeaderName))
                {
                    ret.Name = line.Substring(HeaderName.Length);
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

            if (string.IsNullOrEmpty(ret.Name))
            {
                ret.Name = ret.ID;
            }

            ret.PieceEntries = pieceEntries.ToArray();
            ret.SnapPoints = snapPoints.ToArray();

            return ret;
        }

        /// <summary>
        ///     Creates a string array of this blueprint instance in format <see cref="Format.Blueprint"/>.
        /// </summary>
        /// <returns>A string array representation of this blueprint without the thumbnail</returns>
        public string[] ToArray()
        {
            if (PieceEntries == null)
            {
                return null;
            }

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

        /// <summary>
        ///     Creates a BLOB of this blueprint instance as <see cref="Format.Blueprint"/>.
        /// </summary>
        /// <returns>A byte array representation of this blueprint including the thumbnail</returns>
        public byte[] ToBlob()
        {
            string[] lines = ToArray();
            if (lines == null || lines.Length == 0)
            {
                return null;
            }

            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(lines.Length);
                    foreach (string line in lines)
                    {
                        writer.Write(line);
                    }

                    if (Thumbnail == null)
                    {
                        writer.Write(0);
                    }
                    else
                    {
                        byte[] thumbBytes = Thumbnail.EncodeToPNG();
                        writer.Write(thumbBytes.Length);
                        writer.Write(thumbBytes);
                    }
                }
                return m.ToArray();
            }
        }

        /// <summary>
        ///     Creates a <see cref="ZPackage"/> from this blueprint including the ID and the instance.
        /// </summary>
        /// <returns></returns>
        public ZPackage ToZPackage()
        {
            ZPackage package = new ZPackage();
            package.Write(ID);
            package.Write(ToBlob());
            return package;
        }

        /// <summary>
        ///     Save this instance as a blueprint file to <see cref="FileLocation"/>. 
        ///     Renames the .vbuild file to .blueprint if it was read as one.
        /// </summary>
        /// <returns>true if the blueprint could be saved</returns>
        public bool ToFile()
        {
            string[] lines = ToArray();
            if (lines == null || lines.Length == 0)
            {
                return false;
            }

            using (TextWriter tw = new StreamWriter(FileLocation))
            {
                foreach (string line in lines)
                {
                    tw.WriteLine(line);
                }
                Logger.LogDebug($"Wrote {PieceEntries.Length} pieces to {FileLocation}");
            }

            if (FileFormat == Format.VBuild)
            {
                string newLocation = FileLocation.Replace(".vbuild",".blueprint");
                File.Move(FileLocation, newLocation);
                FileLocation = newLocation;
                FileFormat = Format.Blueprint;
            }

            if (Thumbnail != null)
            {
                File.WriteAllBytes(IconLocation, Thumbnail.EncodeToPNG());
                Logger.LogDebug($"Wrote thumbnail data to {IconLocation}");
            }

            return true;
        }

        public override string ToString()
        {
            return $"{ID} ({GetPieceCount()} pieces)";
        }

        public string ToGUIString()
        {
            return $"<b>{Name}</b>\n({GetPieceCount()} pieces)";
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
        ///     Get the bounds of this blueprint
        /// </summary>
        /// <returns></returns>
        public Bounds GetBounds()
        {
            Bounds bounds = new Bounds();
            foreach(PieceEntry entry in PieceEntries)
            {
                bounds.Encapsulate(entry.GetPosition());
            }
            return bounds;
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

            foreach (var piece in BlueprintManager.Instance.GetPiecesInRadius(position, radius))
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
                        Logger.LogWarning($"Multiple center points! Ignoring @ {piece.transform.position}");
                    }
                    WearNTear wearNTear = piece.GetComponent<WearNTear>();
                    wearNTear.Remove();
                    continue;
                }
                if (!BlueprintManager.Instance.CanCapture(piece))
                {
                    Logger.LogWarning($"Ignoring piece {piece}, not able to make Plan");
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

            // Select and order instance piece entries
            var pieces = collected
                    .OrderBy(x => x.transform.position.y)
                    .ThenBy(x => x.transform.position.x)
                    .ThenBy(x => x.transform.position.z);

            // Create instance piece entries
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

            // Create instance snap points
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
        ///     Creates a prefab from this blueprint, instantiating the stub piece and all pieces
        ///     used in this blueprint. Adds it to the <see cref="ZNetScene"/> and rune <see cref="PieceTable"/>.
        /// </summary>
        /// <returns>true if the prefab could be created</returns>
        public bool CreatePiece()
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
            var stub = PrefabManager.Instance.GetPrefab(PieceBlueprintName);
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

            // Instantiate child objects
            /*if (!GhostInstantiate(Prefab))
            {
                Logger.LogWarning("Could not create prefab");
                Object.DestroyImmediate(Prefab);
                return false;
            }*/

            // Set piece information
            Piece piece = Prefab.GetComponent<Piece>();
            piece.m_name = Name;
            piece.m_enabled = true;
            piece.m_description += "\nFile name: " + Path.GetFileName(FileLocation);
            if (!string.IsNullOrEmpty(Creator))
            {
                piece.m_description += "\nCreator: " + Creator;
            }
            if (!string.IsNullOrEmpty(Description))
            {
                piece.m_description += "\nDescription: " + Description;
            }
            if (Thumbnail != null)
            {
                piece.m_icon = Sprite.Create(Thumbnail, new Rect(0, 0, Thumbnail.width, Thumbnail.height), Vector2.zero);
            }

            // Add to known pieces
            PieceManager.Instance.RegisterPieceInPieceTable(
                Prefab, BlueprintRunePrefab.PieceTableName, BlueprintRunePrefab.CategoryBlueprints);
            
            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.UpdateKnownRecipesList();
            }

            // Create KeyHint
            KeyHint = new KeyHintConfig
            {
                Item = BlueprintRunePrefab.BlueprintRuneName,
                Piece = PrefabName,
                ButtonConfigs = new[]
                    {
                    new ButtonConfig { Name = BlueprintManager.PlanSwitchButton.Name, HintToken = "$hud_bp_switch_to_plan_mode" },
                    new ButtonConfig { Name = "Attack", HintToken = "$hud_bpplace" },
                    new ButtonConfig { Name = "Shift", HintToken = "$hud_bpflatten" },
                    new ButtonConfig { Name = "Ctrl", HintToken = "$hud_bpdirect" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate1" },
                    new ButtonConfig { Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate2" }
                }
            };
            GUIManager.Instance.AddKeyHint(KeyHint);

            return true;
        }

        /// <summary>
        ///     Instantiate this blueprints placement ghost
        /// </summary>
        /// <returns></returns>
        public bool InstantiateGhost()
        {
            if (!Prefab)
            {
                return false;
            }
            if (Prefab.transform.childCount > 1)
            {
                return true;
            }

            GameObject baseObject = Prefab;
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

                // Tiny collider for accurate placement
                GameObject gameObject = new GameObject(PlaceColliderName);
                gameObject.transform.SetParent(baseObject.transform);
                gameObject.layer = LayerMask.NameToLayer("piece_nonsolid");
                SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.002f;
              
                var tf = baseObject.transform;
                var quat = Quaternion.Euler(0, tf.rotation.eulerAngles.y, 0); 

                var prefabs = new Dictionary<string, GameObject>();
                foreach (var piece in pieces.GroupBy(x => x.name).Select(x => x.FirstOrDefault()))
                {
                    var go = PrefabManager.Instance.GetPrefab(piece.name);
                    if (!go)
                    {
                        throw new Exception($"No prefab found for {piece.name}! You are probably missing a dependency for blueprint {Name}");
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
                    var piecePosition = tf.position + piece.GetPosition();


                    GameObject pieceObject = new GameObject($"piece_entry ({i})");
                    pieceObject.transform.SetParent(tf);
                    pieceObject.transform.rotation = piece.GetRotation();
                    pieceObject.transform.position = piecePosition;

                    if (prefabs.TryGetValue(piece.name, out var prefab))
                    {
                        GameObject ghostPrefab;
                        Vector3 ghostPosition;
                        Quaternion ghostRotation;
                        if (prefab.TryGetComponent(out WearNTear wearNTear) && wearNTear.m_new)
                        {
                            // Only instantiate the visual part
                            ghostPrefab = wearNTear.m_new;
                            ghostRotation = ghostPrefab.transform.localRotation;
                            ghostPosition = ghostPrefab.transform.localPosition;
                        }
                        else
                        {
                            // No WearNTear?? Just use the entire prefab
                            ghostPrefab = prefab;
                            ghostRotation = Quaternion.identity;
                            ghostPosition = Vector3.zero;
                        }

                        var child = Object.Instantiate(ghostPrefab, pieceObject.transform);
                        child.transform.localRotation = ghostRotation;
                        child.transform.localPosition = ghostPosition;
                        PrepareGhostPiece(child);

                        // Doors have a dynamic object that also needs to be added
                        if (prefab.TryGetComponent(out Door door))
                        {
                            GameObject doorPrefab = door.m_doorObject;
                            var doorChild = Object.Instantiate(doorPrefab, pieceObject.transform);
                            doorChild.transform.localRotation = doorPrefab.transform.localRotation;
                            doorChild.transform.localPosition = doorPrefab.transform.localPosition;
                            PrepareGhostPiece(doorChild);
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
        private void PrepareGhostPiece(GameObject child)
        {
            // A Ghost doesn't need fancy scripts
            foreach (var component in child.GetComponentsInChildren<MonoBehaviour>())
            {
                Object.Destroy(component);
            }

            // Also no fancy colliders
            foreach(var collider in child.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(collider);
            }

            // Disable ripple effect on ghost (only visible when using Skuld crystal)
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
        }

        /// <summary>
        ///     Removes and destroys this blueprints prefab, KeyHint and files from the game and filesystem.
        /// </summary>
        public void DestroyBlueprint()
        {
            // Remove and destroy prefab
            if (Prefab)
            {
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
                if (PieceManager.Instance.GetPiece(PrefabName) != null)
                {
                    PieceManager.Instance.RemovePiece(PrefabName);
                }
                PrefabManager.Instance.DestroyPrefab(PrefabName);

                // Remove from known recipes
                if (Player.m_localPlayer && Player.m_localPlayer.m_knownRecipes.Contains(PrefabName))
                {
                    Player.m_localPlayer.m_knownRecipes.Remove(PrefabName);
                }
            }

            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }

            // Remove KeyHint
            if (KeyHint != null)
            {
                GUIManager.Instance.RemoveKeyHint(KeyHint);
                KeyHint = null;
            }

            // Delete files
            if (File.Exists(FileLocation))
            {
                File.Delete(FileLocation);
            }
            if (File.Exists(IconLocation))
            {
                File.Delete(IconLocation);
            }
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
                string playerName = Player.m_localPlayer.GetPlayerName();
                string fileName = string.Concat(text.Split(Path.GetInvalidFileNameChars()));
                
                newbp.ID = $"{playerName}_{fileName}".Trim();
                newbp.PrefabName = $"{PieceBlueprintName}:{newbp.ID}";
                newbp.Name = text;
                newbp.Creator = playerName;
                newbp.FileLocation = Path.Combine(BlueprintConfig.blueprintSaveDirectoryConfig.Value, newbp.ID + ".blueprint");
                newbp.IconLocation = newbp.FileLocation.Replace(".blueprint", ".png");
                if (newbp.ToFile())
                {
                    if (BlueprintManager.LocalBlueprints.ContainsKey(newbp.ID))
                    {
                        Blueprint oldbp = BlueprintManager.LocalBlueprints[newbp.ID];
                        oldbp.DestroyBlueprint();
                        BlueprintManager.LocalBlueprints.Remove(newbp.ID);
                    }

                    PlanBuildPlugin.Instance.StartCoroutine(AddBlueprint());
                }
            }

            /// <summary>
            ///     Take screenshot, create the prefab and add the blueprint to the manager as a <see cref="Coroutine"/>.
            /// </summary>
            /// <returns><see cref="IEnumerator"/> yields for the <see cref="Coroutine"/></returns>
            public IEnumerator AddBlueprint()
            {
                // Hide console
                Console.instance.m_chatWindow.gameObject.SetActive(false);
                Console.instance.Update();
                
                // Hide Hud if active
                bool oldHud = Hud.instance.m_userHidden;
                Hud.instance.m_userHidden = true;
                Hud.instance.SetVisible(false);
                Hud.instance.Update();

                // Remove SelectionCircle
                BlueprintManager.Instance.ShowSelectionCircle = false;

                yield return new WaitForEndOfFrame();

                // Get a screenshot
                Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

                // Calculate proper height
                int width = 160;
                int height = (int)Math.Round(160f * screenshot.height / screenshot.width);

                // Create thumbnail image from screenshot
                newbp.Thumbnail = new Texture2D(width, height);
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var xp = 1f * x / width;
                        var yp = 1f * y / height;
                        var xo = (int)Mathf.Round(xp * screenshot.width); // Other X pos
                        var yo = (int)Mathf.Round(yp * screenshot.height); // Other Y pos
                        Color origPixel = screenshot.GetPixel(xo, yo);
                        origPixel.a = 1f;
                        newbp.Thumbnail.SetPixel(x, y, origPixel);
                    }
                }
                newbp.Thumbnail.Apply();

                // Save to file
                File.WriteAllBytes(newbp.IconLocation, newbp.Thumbnail.EncodeToPNG());

                // Destroy properly
                Object.Destroy(screenshot);

                // Reactivate SelectionCircle
                BlueprintManager.Instance.ShowSelectionCircle = true;

                // Reactivate Hud if it was active
                Hud.instance.m_userHidden = oldHud;
                Hud.instance.SetVisible(true);
                Hud.instance.Update();

                yield return new WaitForEndOfFrame();

                // Create and load blueprint prefab
                newbp.CreatePiece();
                BlueprintManager.LocalBlueprints.Add(newbp.ID, newbp);
                BlueprintGUI.ReloadBlueprints(BlueprintLocation.Local);

                Logger.LogInfo("Blueprint created");

                newbp = null;
            }
        }
    }
}