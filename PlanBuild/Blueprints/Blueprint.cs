using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Object = UnityEngine.Object;
using Logger = Jotunn.Logger;
using PlanBuild.Plans;

namespace PlanBuild.Blueprints
{
    internal class Blueprint
    {
        /// <summary>
        ///     Name of the blueprint instance. Translates to &lt;m_name&gt;.blueprint in the filesystem
        /// </summary>
        private string m_name;

        /// <summary>
        ///     Array of the pieces this blueprint is made of
        /// </summary>
        internal PieceEntry[] m_pieceEntries;

        /// <summary>
        ///     Dynamically generated prefab for this blueprint
        /// </summary>
        private GameObject m_prefab;

        /// <summary>
        ///     Name of the generated prefab of the blueprint instance. Is always "piece_blueprint (&lt;m_name&gt;)"
        /// </summary>
        private string m_prefabname;

        /// <summary>
        ///     New "empty" Blueprint with a name but without any pieces. Call Capture() or Load() to add pieces to the blueprint.
        /// </summary>
        /// <param name="name"></param>
        public Blueprint(string name)
        {
            m_name = name;
            m_prefabname = $"piece_blueprint ({name})";
        }

        /// <summary>
        ///     Number of pieces currently stored in this blueprint
        /// </summary>
        /// <returns></returns>
        public int GetPieceCount()
        {
            return m_pieceEntries.Count();
        }

        public bool Capture(Vector3 startPosition, float startRadius, float radiusDelta)
        {
            var vec = startPosition;
            var rot = Camera.main.transform.rotation.eulerAngles;
            Logger.LogDebug("Collecting piece information");

            var numPieces = 0;
            var collected = new List<Piece>();

            collected.Clear();

            foreach (var piece in Piece.m_allPieces)
            {
                if (Vector2.Distance(new Vector2(startPosition.x, startPosition.z), new Vector2(piece.transform.position.x, piece.transform.position.z)) <
                    startRadius && piece.transform.position.y >= startPosition.y)
                {
                    piece.GetComponent<WearNTear>()?.Highlight();
                    collected.Add(piece);
                    numPieces++;
                }
            }

            Logger.LogDebug($"Found {numPieces} in a radius of {startRadius:F2}");

            // Relocate Z
            var minZ = 9999999.9f;
            var minX = 9999999.9f;
            var minY = 9999999.9f;

            foreach (var piece in collected.Where(x => x.m_category != Piece.PieceCategory.Misc && x.IsPlacedByPlayer()))
            {
                minX = Math.Min(piece.m_nview.GetZDO().m_position.x, minX);
                minZ = Math.Min(piece.m_nview.GetZDO().m_position.z, minZ);
                minY = Math.Min(piece.m_nview.GetZDO().m_position.y, minY);
            }

            Logger.LogDebug($"{minX} - {minY} - {minZ}");

            var bottomleft = new Vector3(minX, minY, minZ);

            // select and order instance piece entries
            var pieces = collected.Where(x => x.IsPlacedByPlayer() && x.m_category != Piece.PieceCategory.Misc)
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
                var pos = new Vector3(piece.m_nview.GetZDO().m_position.x - bottomleft.x, piece.m_nview.GetZDO().m_position.y - bottomleft.y,
                    piece.m_nview.GetZDO().m_position.z - bottomleft.z);

                var quat = piece.m_nview.GetZDO().m_rotation;
                quat.eulerAngles = piece.transform.eulerAngles;

                var additionalInfo = piece.GetComponent<TextReceiver>() != null ? piece.GetComponent<TextReceiver>().GetText() : "";

                string pieceName = piece.name.Split('(')[0];
                if (pieceName.EndsWith(PlanPiecePrefab.plannedSuffix))
                {
                    pieceName = pieceName.Replace(PlanPiecePrefab.plannedSuffix, null);

                }
                m_pieceEntries[i++] = new PieceEntry(pieceName, piece.m_category.ToString(), pos, quat, additionalInfo);
            }

            return true;
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
            File.WriteAllBytes(Path.Combine(BlueprintManager.BlueprintPath, m_name + ".png"), thumbnail.EncodeToPNG());

            // Destroy properly
            Object.Destroy(screenShot);
            Object.Destroy(thumbnail);
        }

        public bool Save()
        {
            if (m_pieceEntries == null)
            {
                Logger.LogWarning("No pieces stored to save");
            }
            else
            {
                using (TextWriter tw = new StreamWriter(Path.Combine(BlueprintManager.BlueprintPath, m_name + ".blueprint")))
                {
                    foreach (var piece in m_pieceEntries)
                    {
                        tw.WriteLine(piece.line);
                    }

                    Logger.LogDebug("Wrote " + m_pieceEntries.Length + " pieces to " + Path.Combine(BlueprintManager.BlueprintPath, m_name + ".blueprint"));
                }
            }

            return true;
        }

        public bool Load(string fileLocation)
        {
            try
            { 
                string extension = Path.GetExtension(fileLocation).ToLowerInvariant();
                 
                var lines = File.ReadAllLines(fileLocation).ToList();
                Logger.LogDebug("read " + lines.Count + " pieces from " + Path.Combine(BlueprintManager.BlueprintPath, m_name + ".blueprint"));

                if (m_pieceEntries == null)
                {
                    m_pieceEntries = new PieceEntry[lines.Count()];
                }
                else if (m_pieceEntries.Length > 0)
                {
                    Array.Clear(m_pieceEntries, 0, m_pieceEntries.Length - 1);
                    Array.Resize(ref m_pieceEntries, lines.Count());
                }

                uint i = 0;
                foreach (var line in lines)
                {
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
                            Logger.LogWarning("Unknown extension " + extension);
                            return false;
                    }

                    m_pieceEntries[i++] = pieceEntry;
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return false;
            }
        }

        public void CalculateCost()
        {
            if (m_pieceEntries == null)
            {
                Logger.LogWarning("No pieces loaded");
                return;
            }


        }

        public GameObject CreatePrefab()
        {
            if (m_prefab != null)
            {
                return m_prefab;
            }

            Logger.LogInfo($"Creating dynamic prefab {m_prefabname}");

            if (m_pieceEntries == null)
            {
                Logger.LogWarning("No pieces loaded");
                return null;
            }

            // Get Stub from PrefabManager
            var stub = PrefabManager.Instance.GetPrefab("piece_blueprint");
            if (stub == null)
            {
                Logger.LogWarning("Could not load blueprint stub from prefabs");
                return null;
            }

            // Instantiate clone from stub
            ZNetView.m_forceDisableInit = true;
            m_prefab = Object.Instantiate(stub);
            ZNetView.m_forceDisableInit = false;
            m_prefab.name = m_prefabname;

            var piece = m_prefab.GetComponent<Piece>();

            if (File.Exists(Path.Combine(BlueprintManager.BlueprintPath, m_name + ".png")))
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(Path.Combine(BlueprintManager.BlueprintPath, m_name + ".png")));

                piece.m_icon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            }

            piece.m_name = m_name;
            piece.m_enabled = true;

            // Instantiate child objects
            if (!GhostInstantiate(m_prefab))
            {
                Logger.LogWarning("Could not create prefab");
                Object.DestroyImmediate(m_prefab);
                return null;
            }

            // Add to known prefabs

            CustomPiece CP = new CustomPiece(m_prefab, new PieceConfig
            {
                PieceTable = "_BlueprintPieceTable"
            });
            PieceManager.Instance.AddPiece(CP);

            return m_prefab;
        }

        public void AddToPieceTable()
        {
            if (m_prefab == null)
            {
                return;
            }

            var table = PieceManager.Instance.GetPieceTable("_BlueprintPieceTable");
            if (table == null)
            {
                Logger.LogWarning("BlueprintPieceTable not found");
                return;
            }

            if (!table.m_pieces.Contains(m_prefab))
            {
                Logger.LogInfo($"Adding {m_prefabname} to BlueprintRune");

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
            var table = PieceManager.Instance.GetPieceTable("_BlueprintPieceTable");
            if (table == null)
            {
                Logger.LogWarning("BlueprintPieceTable not found");
                return;
            }

            if (table.m_pieces.Contains(m_prefab))
            {
                Logger.LogInfo($"Removing {m_prefabname} from BlueprintRune");

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

                var tf = baseObject.transform;
                tf.rotation = Camera.main.transform.rotation;
                var quat = Quaternion.Euler(0, tf.rotation.eulerAngles.y, 0);
                tf.SetPositionAndRotation(tf.position, quat);
                tf.position -= tf.right * (maxX / 2f);
                tf.position += tf.forward * 5f;

                var prefabs = new Dictionary<string, GameObject>();
                foreach (var piece in pieces.GroupBy(x => x.name).Select(x => x.FirstOrDefault()))
                {
                    var go = PrefabManager.Instance.GetPrefab(piece.name);
                    go.transform.SetPositionAndRotation(go.transform.position, quat);
                    prefabs.Add(piece.name, go);
                }

                var nulls = prefabs.Values.Count(x => x == null);
                if (nulls > 0)
                {
                    throw new Exception($"{nulls} nulls found");
                }

                foreach (var piece in pieces)
                {
                    var pos = tf.position + tf.right * piece.GetPosition().x + tf.forward * piece.GetPosition().z +
                      new Vector3(0, piece.GetPosition().y, 0);

                    var q = new Quaternion();
                    q.eulerAngles = new Vector3(0, tf.transform.rotation.eulerAngles.y + piece.GetRotation().eulerAngles.y);

                    var child = Object.Instantiate(prefabs[piece.name], pos, q);
                    child.transform.SetParent(baseObject.transform);

                    // A Ghost doesn't need fancy scripts
                    foreach (var component in child.GetComponentsInChildren<MonoBehaviour>())
                    {
                        Object.Destroy(component);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while instantiating: {ex}");
                ret = false;
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }

            return ret;
        }

        public Vector2 GetExtent()
        {
            return new Vector2(m_pieceEntries.Max(x => x.posX), m_pieceEntries.Max(x => x.posZ));
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
                newbp.m_prefabname = $"piece_blueprint ({newbp.m_name})";
                if (newbp.Save())
                {
                    if (BlueprintManager.Instance.m_blueprints.ContainsKey(newbp.m_name))
                    {
                        Blueprint oldbp = BlueprintManager.Instance.m_blueprints[newbp.m_name];
                        oldbp.Destroy();
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