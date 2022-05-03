using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using PlanBuild.Blueprints.Tools;
using PlanBuild.Plans;
using PlanBuild.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Logger = Jotunn.Logger;
using Object = UnityEngine.Object;
using ShaderHelper = PlanBuild.Utils.ShaderHelper;

namespace PlanBuild.Blueprints
{
    internal class Blueprint
    {
        public const string PieceBlueprintName = "piece_blueprint";
        public static int PieceBlueprintHash = PieceBlueprintName.GetStableHashCode();
        public const string PlaceColliderName = "place_collider";
        public const string AdditionalInfo = "AdditionalText";

        private const string HeaderName = "#Name:";
        private const string HeaderCreator = "#Creator:";
        private const string HeaderDescription = "#Description:";
        private const string HeaderCategory = "#Category:";
        private const string HeaderSnapPoints = "#SnapPoints";
        private const string HeaderPieces = "#Pieces";
        private const int ThumbnailSize = 256;

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
        public string ThumbnailLocation;

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
        ///     Optional category for this blueprint. Defaults to "Blueprints".
        /// </summary>
        public string Category = BlueprintAssets.CategoryBlueprints;

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
        public Texture2D Thumbnail
        {
            get => ResizedThumbnail;
            set
            {
                if (value.width > ThumbnailSize)
                {
                    ShaderHelper.ScaleTexture(value, ThumbnailSize);
                }
                ResizedThumbnail = value;
            }
        }

        /// <summary>
        ///     Internal representation of the Thumbnail, always resized to max 160 width
        /// </summary>
        private Texture2D ResizedThumbnail;

        /// <summary>
        ///     Name of the generated prefab of the blueprint instance. Is always "piece_blueprint:{ID}"
        /// </summary>
        private string PrefabName => $"{PieceBlueprintName}:{ID}";

        /// <summary>
        ///     Dynamically generated prefab for this blueprint
        /// </summary>
        private GameObject Prefab;

        /// <summary>
        ///     Dynamically generated KeyHint for this blueprint
        /// </summary>
        private KeyHintConfig KeyHint;

        /// <summary>
        ///     Bounds of this blueprint
        /// </summary>
        private Bounds Bounds;

        /// <summary>
        ///     TTL timer for the ghost prefab
        /// </summary>
        internal float GhostActiveTime;

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
            ret.ThumbnailLocation = fileLocation.Replace(extension, ".png");

            if (File.Exists(ret.ThumbnailLocation))
            {
                ret.Thumbnail = AssetUtils.LoadTexture(ret.ThumbnailLocation, relativePath: false);
                Logger.LogDebug($"Read thumbnail data from {ret.ThumbnailLocation}");
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
        ///     Create a blueprint instance with a given ID from a compressed BLOB.
        /// </summary>
        /// <param name="id">The unique blueprint ID</param>
        /// <param name="payload">BLOB with blueprint data</param>
        /// <returns><see cref="Blueprint"/> instance with an optional thumbnail</returns>
        public static Blueprint FromBlob(string id, byte[] payload)
        {
            Blueprint ret;
            List<string> lines = new List<string>();
            using MemoryStream m = new MemoryStream(global::Utils.Decompress(payload));
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
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(thumbnailBytes);
                    ret.Thumbnail = tex;
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
            ret.FileFormat = Format.Blueprint;
            ret.FileLocation = Path.Combine(Config.BlueprintSaveDirectoryConfig.Value, $"{id}.blueprint");
            ret.ThumbnailLocation = Path.Combine(Config.BlueprintSaveDirectoryConfig.Value, $"{id}.png");

            List<PieceEntry> pieceEntries = new List<PieceEntry>();
            List<SnapPoint> snapPoints = new List<SnapPoint>();

            ParserState state = ParserState.Pieces;

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
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
                    if (ret.Description.StartsWith("\""))
                    {
                        ret.Description = SimpleJson.SimpleJson.DeserializeObject<string>(ret.Description);
                    }
                    continue;
                }
                if (line.StartsWith(HeaderCategory))
                {
                    ret.Category = line.Substring(HeaderCategory.Length);
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
                if (line.StartsWith("#"))
                {
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
            ret.Add(HeaderDescription + SimpleJson.SimpleJson.SerializeObject(Description));
            ret.Add(HeaderCategory + Category);
            if (SnapPoints.Any())
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
        ///     Creates a compressed BLOB of this blueprint instance as <see cref="Format.Blueprint"/>.
        /// </summary>
        /// <returns>A byte array representation of this blueprint including the thumbnail</returns>
        public byte[] ToBlob()
        {
            string[] lines = ToArray();
            if (lines == null || lines.Length == 0)
            {
                return null;
            }

            using MemoryStream m = new MemoryStream();
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
            return global::Utils.Compress(m.ToArray());
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
                string newLocation = FileLocation.Replace(".vbuild", ".blueprint");
                File.Move(FileLocation, newLocation);
                FileLocation = newLocation;
                FileFormat = Format.Blueprint;
            }

            if (Thumbnail != null)
            {
                File.WriteAllBytes(ThumbnailLocation, Thumbnail.EncodeToPNG());
                Logger.LogDebug($"Wrote thumbnail data to {ThumbnailLocation}");
            }

            return true;
        }

        public override string ToString()
        {
            return Localization.instance.Localize($"{ID} ($gui_bpmarket_pieces)", GetPieceCount().ToString());
        }

        public string ToGUIString()
        {
            return Localization.instance.Localize($"<b>{Name}</b>\n ($gui_bpmarket_pieces)", GetPieceCount().ToString());
        }

        /// <summary>
        ///     Number of pieces currently stored in this blueprint
        /// </summary>
        /// <returns></returns>
        public int GetPieceCount()
        {
            return PieceEntries.Length;
        }

        /// <summary>
        ///     Get the bounds of this blueprint
        /// </summary>
        /// <returns></returns>
        public Bounds GetBounds()
        {
            if (Bounds.size.magnitude != 0)
            {
                return Bounds;
            }
            foreach (PieceEntry entry in PieceEntries)
            {
                Bounds.Encapsulate(entry.GetPosition());
            }
            return Bounds;
        }

        /// <summary>
        ///     Capture all pieces in the selection
        /// </summary>
        /// <param name="selection">Selection</param>
        /// <returns></returns>
        public bool Capture(Selection selection)
        {
            Logger.LogDebug("Collecting piece information");

            var numPieces = 0;
            var collected = new List<Piece>();
            var snapPoints = new List<Vector3>();
            Transform centerPiece = null;

            foreach (var zdoid in selection)
            {
                GameObject selected = BlueprintManager.Instance.GetGameObject(zdoid, true);
                if (selected.name.StartsWith(BlueprintAssets.PieceSnapPointName))
                {
                    snapPoints.Add(selected.transform.position);
                    WearNTear wearNTear = selected.GetComponent<WearNTear>();
                    wearNTear.Remove();
                    continue;
                }
                if (selected.name.StartsWith(BlueprintAssets.PieceCenterPointName))
                {
                    if (centerPiece == null)
                    {
                        centerPiece = selected.transform;
                    }
                    else
                    {
                        Logger.LogWarning($"Multiple center points! Ignoring @ {selected.transform.position}");
                    }
                    WearNTear wearNTear = selected.GetComponent<WearNTear>();
                    wearNTear.Remove();
                    continue;
                }
                Piece piece = selected.GetComponent<Piece>();
                if (!BlueprintManager.Instance.CanCapture(piece))
                {
                    Logger.LogWarning($"Ignoring piece {piece}, not able to make blueprint");
                    continue;
                }
                collected.Add(piece);
                numPieces++;
            }

            if (!collected.Any())
            {
                return false;
            }

            Logger.LogDebug($"Found {numPieces} pieces");

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
            var piecesCount = pieces.Count();

            // Create instance piece entries
            if (PieceEntries == null)
            {
                PieceEntries = new PieceEntry[piecesCount];
            }
            else if (PieceEntries.Length > 0)
            {
                Array.Clear(PieceEntries, 0, PieceEntries.Length - 1);
                Array.Resize(ref PieceEntries, piecesCount);
            }

            uint i = 0;
            foreach (var piece in pieces)
            {
                var pos = piece.m_nview.GetZDO().m_position - center;

                var quat = piece.m_nview.GetZDO().m_rotation;
                quat.eulerAngles = piece.transform.eulerAngles;

                var additionalInfo = piece.GetComponent<TextReceiver>() != null ? piece.GetComponent<TextReceiver>().GetText() : "";

                var scale = piece.transform.localScale;

                string pieceName = piece.name.Split('(')[0];
                if (piece.gameObject.GetComponent<ZNetView>() is { m_zdo: { } } znet &&
                    ZNetScene.instance.GetPrefab(znet.m_zdo.m_prefab) is { } prefab)
                {
                    pieceName = prefab.name;
                }
                if (pieceName.EndsWith(PlanPiecePrefab.PlannedSuffix))
                {
                    pieceName = pieceName.Replace(PlanPiecePrefab.PlannedSuffix, null);
                }
                PieceEntries[i++] = new PieceEntry(pieceName, piece.m_category.ToString(), pos, quat, additionalInfo, scale);
            }

            // Create instance snap points
            if (SnapPoints == null)
            {
                SnapPoints = new SnapPoint[snapPoints.Count];
            }
            else if (SnapPoints.Length > 0)
            {
                Array.Clear(SnapPoints, 0, SnapPoints.Length - 1);
                Array.Resize(ref SnapPoints, snapPoints.Count);
            }

            for (int j = 0; j < snapPoints.Count; j++)
            {
                SnapPoints[j] = new SnapPoint(snapPoints[j] - center);
            }

            return true;
        }

        /// <summary>
        ///     Creates a prefab from this blueprint, instantiating the stub piece.
        ///     Adds it to the rune <see cref="PieceTable"/> and creates a <see cref="KeyHint"/>.
        /// </summary>
        /// <returns>true if the prefab could be created</returns>
        public bool CreatePiece()
        {
            if (Prefab)
            {
                return false;
            }

            Logger.LogDebug($"Creating dynamic prefab {PrefabName}");

            if (PieceEntries == null)
            {
                Logger.LogWarning("Could not create blueprint prefab: No pieces loaded");
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

            // Set piece information
            Piece piece = Prefab.GetComponent<Piece>();
            piece.m_name = Name;
            piece.m_enabled = true;
            piece.m_description = $"File name: {Path.GetFileName(FileLocation)}";
            if (!string.IsNullOrEmpty(Creator))
            {
                piece.m_description += $"{Environment.NewLine}Creator: {Creator}";
            }
            if (!string.IsNullOrEmpty(Description))
            {
                piece.m_description += $"{Environment.NewLine}Description: {Description}";
            }
            if (Thumbnail != null)
            {
                piece.m_icon = Sprite.Create(Thumbnail, new Rect(0, 0, Thumbnail.width, Thumbnail.height), Vector2.zero);
            }

            // Add to known pieces
            PieceManager.Instance.RegisterPieceInPieceTable(Prefab, BlueprintAssets.PieceTableName, Category);

            // Create KeyHint
            CreateKeyHint();

            // Add PlacementComponent
            Prefab.AddComponent<PlacementComponent>();

            return true;
        }

        public void CreateKeyHint()
        {
            if (KeyHint != null)
            {
                KeyHintManager.Instance.RemoveKeyHint(KeyHint);
            }
            KeyHint = new KeyHintConfig
            {
                Item = BlueprintAssets.BlueprintRuneName,
                Piece = PrefabName,
                ButtonConfigs = new[]
                {
                    new ButtonConfig
                    {
                        Name = "Attack", HintToken = "$hud_bpplace"
                    },
                    new ButtonConfig
                    {
                        Name = Config.RadiusModifierButton.Name, Config = Config.RadiusModifierConfig,
                        HintToken = Config.DirectBuildDefault ? "$hud_bpplanned" : "$hud_bpdirect"
                    },
                    new ButtonConfig
                    {
                        Name = Config.CameraModifierButton.Name, Config = Config.CameraModifierConfig,
                        HintToken = "$hud_bpcamera"
                    },
                    new ButtonConfig
                    {
                        Name = "Scroll", Axis = "Mouse ScrollWheel", HintToken = "$hud_bprotate1"
                    },
                }
            };
            KeyHintManager.Instance.AddKeyHint(KeyHint);
        }

        /// <summary>
        ///     Create a thumbnail from the piece prefab and write it to <see cref="ThumbnailLocation"/>
        /// </summary>
        /// <param name="additionalRotation">Rotation added to the base rotation of the rendered prefab on the Y-axis</param>
        public bool CreateThumbnail(int additionalRotation = 0)
        {
            if (!InstantiateGhost())
            {
                return false;
            }

            var req = new RenderManager.RenderRequest(Prefab)
            {
                Rotation = RenderManager.IsometricRotation * Quaternion.Euler(0f, additionalRotation, 0f),
                Width = ThumbnailSize,
                Height = ThumbnailSize
            };

            var sprite = RenderManager.Instance.Render(req);

            if (sprite == null)
            {
                return false;
            }

            Thumbnail = sprite.texture;
            File.WriteAllBytes(ThumbnailLocation, Thumbnail.EncodeToPNG());
            Prefab.GetComponent<Piece>().m_icon = Sprite.Create(Thumbnail, new Rect(0, 0, Thumbnail.width, Thumbnail.height), Vector2.zero);

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

                var prefabs = new Dictionary<string, GameObject>();
                foreach (var piece in pieces.GroupBy(x => x.name).Select(x => x.FirstOrDefault()))
                {
                    var go = PrefabManager.Instance.GetPrefab(piece.name);
                    if (!go)
                    {
                        Logger.LogWarning($"No prefab found for {piece.name}! You are probably missing a dependency for blueprint {Name}");
                        continue;
                    }
                    prefabs.Add(piece.name, go);
                }

                for (int i = 0; i < pieces.Count; i++)
                {
                    PieceEntry piece = pieces[i];
                    try
                    {
                        var piecePosition = tf.position + piece.GetPosition();
                        var pieceRotation = tf.rotation * piece.GetRotation();
                        var pieceScale = piece.GetScale();

                        if (prefabs.TryGetValue(piece.name, out var prefab))
                        {
                            var child = Object.Instantiate(prefab, piecePosition, pieceRotation, tf);
                            child.transform.localScale = pieceScale;
                            PrepareGhostPiece(child);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Error while creating ghost of line: {piece.line}\n{e}");
                    }
                }

                if (Config.ShowGridConfig.Value)
                {
                    DebugUtils.InitLaserGrid(baseObject, GetBounds());
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error caught while instantiating {Name}: {ex}");
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
                Object.DestroyImmediate(component);
            }

            // Also no fancy colliders
            foreach (var collider in child.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            // Remove original snap points
            foreach (var tf in child.GetComponentsInChildren<Transform>(true))
            {
                if (tf.name.StartsWith("_snappoint"))
                {
                    Object.DestroyImmediate(tf.gameObject);
                }
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

        public void DestroyGhost()
        {
            if (!Prefab)
            {
                return;
            }
            if (Prefab.transform.childCount <= 1)
            {
                return;
            }

            foreach (Transform transform in Prefab.transform)
            {
                if (transform.name != "_GhostOnly")
                {
                    Object.Destroy(transform.gameObject);
                }
            }

            GhostActiveTime = 0f;
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
                var table = PieceManager.Instance.GetPieceTable(BlueprintAssets.PieceTableName);
                if (table == null)
                {
                    Logger.LogWarning($"{BlueprintAssets.PieceTableName} not found");
                    return;
                }
                if (table.m_pieces.Contains(Prefab))
                {
                    Logger.LogInfo($"Removing {PrefabName} from {BlueprintAssets.BlueprintRuneName}");

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

                Prefab = null;
            }

            if (Player.m_localPlayer)
            {
                Player.m_localPlayer.UpdateAvailablePiecesList();
            }

            // Remove KeyHint
            if (KeyHint != null)
            {
                KeyHintManager.Instance.RemoveKeyHint(KeyHint);
                KeyHint = null;
            }

            // Delete files
            if (File.Exists(FileLocation))
            {
                File.Delete(FileLocation);
            }
            if (File.Exists(ThumbnailLocation))
            {
                File.Delete(ThumbnailLocation);
            }
        }
    }
}