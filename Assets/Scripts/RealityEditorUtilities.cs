using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using Unity.Collections;

using UnityEngine.Rendering; // MeshData APIs



namespace RealityEditor
{
    public enum GenerateType
    {
        Add,
        Reconstruction,

        Instruction,

        Sketch,


        None

    }



    public enum MeshType
    {
        Generated,
        TargetObject,
        BackGround,
        Modified

    }
    
    
   public static class FastBoundsColliderBuilder
    {
        public enum PlanBShape { Box, CapsuleAutoAxis, Sphere }

        /// <summary>
        /// Ultra-fast "Plan B": build ONE primitive collider from combined Renderer bounds.
        /// Now with orientation: aligns the collider child to best renderer (or a hint) so
        /// the primitive fits the mesh direction.
        /// </summary>
        public static Collider BuildSingle(
            GameObject root,
            PlanBShape shape = PlanBShape.CapsuleAutoAxis,
            string layerName = "GeneratedObject",
            bool makeSameNamedChild = true,
            bool hideOriginalRenderers = false,
            LuaMonoBehavior lua = null,
            bool alignToBestRenderer = true,
            Transform orientationHint = null   // optional: override which transform to align to
        )
        {
            if (!root) return null;

            // 1) Aggregate world bounds from all renderers under root
            var rens = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (rens == null || rens.Length == 0) return null;

            bool inited = false;
            Bounds wb = default;
            for (int i = 0; i < rens.Length; i++)
            {
                var r = rens[i];
                if (!r) continue;
                if (!inited) { wb = r.bounds; inited = true; }
                else wb.Encapsulate(r.bounds);
            }
            if (!inited) return null;

            // 2) Find/create child with EXACT same name as parent
            Transform child = null;
            if (makeSameNamedChild)
            {
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    var c = root.transform.GetChild(i);
                    if (c && c.name == root.name) { child = c; break; }
                }
                if (!child)
                {
                    var go = new GameObject(root.name);
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    child = go.transform;
                }
            }
            else
            {
                child = root.transform;
            }

            // 3) Optional: set layer on the holder
            int layer = -1;
            if (!string.IsNullOrEmpty(layerName))
            {
                int l = LayerMask.NameToLayer(layerName);
                if (l != -1) layer = l;
            }
            if (layer != -1) child.gameObject.layer = layer;

            // 4) Choose an orientation to align to (best renderer by volume, or a hint)
            Transform refT = orientationHint ? orientationHint : (alignToBestRenderer ? FindLargestRendererTransform(rens) : null);
            if (refT != null)
            {
                // Align child's rotation to the reference renderer so local axes match the mesh
                child.rotation = refT.rotation;
            }

            // 5) Compute oriented center/size in child-local space from the world bounds corners
            Vector3 centerLocal;
            Vector3 sizeLocal;
            ComputeLocalOBBFromWorldBounds(wb, child, out centerLocal, out sizeLocal);

            // 6) Build the single collider using local center/size (already oriented)
            Collider inner = null;
            switch (shape)
            {
                case PlanBShape.Box:
                    {
                        var bc = child.GetComponent<BoxCollider>();
                        if (!bc) bc = child.gameObject.AddComponent<BoxCollider>();
                        bc.center = centerLocal;
                        bc.size = ClampSize(sizeLocal);
                        inner = bc;
                        break;
                    }

                case PlanBShape.Sphere:
                    {
                        var sc = child.GetComponent<SphereCollider>();
                        if (!sc) sc = child.gameObject.AddComponent<SphereCollider>();
                        sc.center = centerLocal;
                        float radius = 0.5f * Mathf.Min(sizeLocal.x, Mathf.Min(sizeLocal.y, sizeLocal.z));
                        sc.radius = Mathf.Max(0.0005f, radius);
                        inner = sc;
                        break;
                    }

                case PlanBShape.CapsuleAutoAxis:
                default:
                    {
                        var cc = child.GetComponent<CapsuleCollider>();
                        if (!cc) cc = child.gameObject.AddComponent<CapsuleCollider>();
                        FitCapsuleFromLocalSize(cc, centerLocal, sizeLocal);
                        inner = cc;
                        break;
                    }
            }

            // 7) Mark and tune
            if (!child.GetComponent<GeneratedColliderMarker>()) child.gameObject.AddComponent<GeneratedColliderMarker>();
            if (layer != -1) child.gameObject.layer = layer;

            // Try to set custom 'excludeLayers' if your project exposes it
            TrySetExcludeLayers(inner, LayerMask.GetMask("GeneratedObject"));

            // 8) Optionally hide visuals (keeps objects active)
            if (hideOriginalRenderers)
            {
                for (int i = 0; i < rens.Length; i++)
                {
                    var r = rens[i];
                    if (!r) continue;
#if UNITY_2021_3_OR_NEWER
                    r.forceRenderingOff = true;
                    if (!r.forceRenderingOff) r.enabled = false;
#else
                    r.enabled = false;
#endif
                }
            }

            // 9) Wire Lua innerCollider so your onCollisionEnter filters correctly
            if (!lua) lua = root.GetComponent<LuaMonoBehavior>() ?? root.GetComponentInParent<LuaMonoBehavior>();
            if (lua) lua.innerCollider = inner;

            return inner;
        }

        // ---------- helpers ----------

        static Transform FindLargestRendererTransform(Renderer[] rens)
        {
            Transform best = null;
            float bestVol = -1f;
            for (int i = 0; i < rens.Length; i++)
            {
                var r = rens[i];
                if (!r) continue;
                var b = r.bounds;
                float vol = b.size.x * b.size.y * b.size.z;
                if (vol > bestVol) { bestVol = vol; best = r.transform; }
            }
            return best;
        }

        static void ComputeLocalOBBFromWorldBounds(Bounds wb, Transform holder, out Vector3 centerLocal, out Vector3 sizeLocal)
        {
            // 8 corners of the world AABB
            var ext = wb.extents;
            var c = wb.center;

            Vector3[] corners =
            {
                new Vector3(c.x - ext.x, c.y - ext.y, c.z - ext.z),
                new Vector3(c.x - ext.x, c.y - ext.y, c.z + ext.z),
                new Vector3(c.x - ext.x, c.y + ext.y, c.z - ext.z),
                new Vector3(c.x - ext.x, c.y + ext.y, c.z + ext.z),
                new Vector3(c.x + ext.x, c.y - ext.y, c.z - ext.z),
                new Vector3(c.x + ext.x, c.y - ext.y, c.z + ext.z),
                new Vector3(c.x + ext.x, c.y + ext.y, c.z - ext.z),
                new Vector3(c.x + ext.x, c.y + ext.y, c.z + ext.z),
            };

            // Transform corners to holder local; find min/max in that space
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < 8; i++)
            {
                var p = holder.InverseTransformPoint(corners[i]);
                if (p.x < min.x) min.x = p.x; if (p.y < min.y) min.y = p.y; if (p.z < min.z) min.z = p.z;
                if (p.x > max.x) max.x = p.x; if (p.y > max.y) max.y = p.y; if (p.z > max.z) max.z = p.z;
            }

            centerLocal = 0.5f * (min + max);
            sizeLocal = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
        }

        static Vector3 ClampSize(Vector3 s)
        {
            const float eps = 0.001f;
            return new Vector3(Mathf.Max(eps, s.x), Mathf.Max(eps, s.y), Mathf.Max(eps, s.z));
        }

        static void FitCapsuleFromLocalSize(CapsuleCollider cc, Vector3 centerLocal, Vector3 sizeLocal)
        {
            cc.center = centerLocal;

            // choose axis by largest local extent
            if (sizeLocal.y >= sizeLocal.x && sizeLocal.y >= sizeLocal.z)
            {
                cc.direction = 1; // Y
                cc.radius = Mathf.Max(0.0005f, 0.5f * Mathf.Min(sizeLocal.x, sizeLocal.z));
                cc.height = Mathf.Max(2f * cc.radius, sizeLocal.y);
            }
            else if (sizeLocal.x >= sizeLocal.y && sizeLocal.x >= sizeLocal.z)
            {
                cc.direction = 0; // X
                cc.radius = Mathf.Max(0.0005f, 0.5f * Mathf.Min(sizeLocal.y, sizeLocal.z));
                cc.height = Mathf.Max(2f * cc.radius, sizeLocal.x);
            }
            else
            {
                cc.direction = 2; // Z
                cc.radius = Mathf.Max(0.0005f, 0.5f * Mathf.Min(sizeLocal.x, sizeLocal.y));
                cc.height = Mathf.Max(2f * cc.radius, sizeLocal.z);
            }
        }

        static void TrySetExcludeLayers(Collider col, int mask)
        {
            if (!col) return;
            var prop = col.GetType().GetProperty("excludeLayers");
            if (prop != null && prop.PropertyType == typeof(int))
            {
                try { prop.SetValue(col, mask, null); } catch { }
            }
        }
    }

   

    /// <summary>
    /// Small sweeper used above (same as we discussed earlier).
    /// </summary>
    public static class MeshColliderSweeper
    {
        public static void CleanupGeneratedColliders(GameObject root, bool removeHullGameObjects = true)
        {
            if (!root) return;

            var cols = root.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                var go = cols[i] ? cols[i].gameObject : null;
                if (!go) continue;
                if (go.GetComponent<GeneratedColliderMarker>() || go.name.StartsWith("Hull_"))
                    UnityEngine.Object.Destroy(cols[i]);
            }

            if (!removeHullGameObjects) return;

            var markers = root.GetComponentsInChildren<GeneratedColliderMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                var go = markers[i] ? markers[i].gameObject : null;
                if (!go) continue;

                bool delete = go.name.StartsWith("Hull_");
                if (!delete)
                {
                    var comps = go.GetComponents<Component>();
                    bool onlyBasic = true;
                    for (int c = 0; c < comps.Length; c++)
                    {
                        var comp = comps[c];
                        if (comp is Transform || comp is MeshCollider || comp is GeneratedColliderMarker) continue;
                        if (comp is Renderer r && (!r.enabled || (Application.unityVersion.StartsWith("2021") && r.forceRenderingOff))) continue;
                        onlyBasic = false; break;
                    }
                    delete = onlyBasic;
                }

                if (delete) UnityEngine.Object.Destroy(go);
            }
        }
    }
    

    

    public static class RuntimeMeshReadabilityPatcher
    {
#if UNITY_2020_2_OR_NEWER
        public static int PatchAllUnder(GameObject root)
        {
            if (!root) return 0;
            int count = 0;

            var mfs = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < mfs.Length; i++)
            {
                var mf = mfs[i];
                var src = mf ? mf.sharedMesh : null;
                if (!src || src.vertexCount == 0) continue;
                if (!src.isReadable)
                {
                    var copy = MakeReadableCopyPublic(src);
                    mf.sharedMesh = copy;
                    count++;
                }
            }

            var smrs = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < smrs.Length; i++)
            {
                var smr = smrs[i];
                var src = smr ? smr.sharedMesh : null;
                if (!src || src.vertexCount == 0) continue;
                if (!src.isReadable)
                {
                    var copy = MakeReadableCopyPublic(src);
                    smr.sharedMesh = copy;
                    count++;
                }
            }
            return count;
        }

        public static Mesh MakeReadableCopyPublic(Mesh src)
        {
            using var ro = Mesh.AcquireReadOnlyMeshData(src);
            var md = ro[0];
            var dst = new Mesh { name = src.name + "_ReadableCopy" };

            var vtx = new NativeArray<Vector3>(md.vertexCount, Allocator.Temp); md.GetVertices(vtx); dst.SetVertices(vtx); vtx.Dispose();
            var nor = new NativeArray<Vector3>(md.vertexCount, Allocator.Temp); md.GetNormals(nor); if (nor.Length > 0) dst.SetNormals(nor); nor.Dispose();
            var uv0 = new NativeArray<Vector2>(md.vertexCount, Allocator.Temp); md.GetUVs(0, uv0); if (uv0.Length > 0) dst.SetUVs(0, uv0); uv0.Dispose();
            var tan = new NativeArray<Vector4>(md.vertexCount, Allocator.Temp); md.GetTangents(tan); if (tan.Length > 0) dst.SetTangents(tan); tan.Dispose();

            dst.subMeshCount = md.subMeshCount;
            for (int s = 0; s < md.subMeshCount; s++)
            {
                var sm = md.GetSubMesh(s);
                var idx = new NativeArray<int>((int)sm.indexCount, Allocator.Temp);
                md.GetIndices(idx, s);
                dst.SetIndices(idx, MeshTopology.Triangles, s, true);
                idx.Dispose();
            }

            dst.bounds = src.bounds;
            dst.UploadMeshData(false);
            return dst;
        }
#else
        public static int PatchAllUnder(GameObject root){ return 0; }
        public static Mesh MakeReadableCopyPublic(Mesh src){ return null; }
#endif
    }

    public static class EarlyGuardUtil
    {
        public static void Run(GameObject root)
        {
            if (!root) return;
            MeshColliderSweeper.CleanupGeneratedColliders(root);
            RuntimeMeshReadabilityPatcher.PatchAllUnder(root);
        }
    }

    


    // ---------------------------------------------------------
    public static class RuntimeConvexPartsBuilder
    {
        public static IEnumerator BuildFromObject(
            GameObject root,
            int parts = 6,
            bool hideOriginalRenderers = true,
            string layerName = "GeneratedObject",
            int batchSize = 1,
            bool wireLuaBehavior = true,
            float frameBudgetMs = 2f
        )
        {
            if (!root) yield break;

            float t0 = Time.realtimeSinceStartup;
            bool ShouldYield()
            {
                if ((Time.realtimeSinceStartup - t0) * 1000f >= frameBudgetMs)
                {
                    t0 = Time.realtimeSinceStartup;
                    return true;
                }
                return false;
            }

            Mesh srcMesh = null;
            GameObject owner = null;
            Matrix4x4 toOwnerLocal = Matrix4x4.identity;
            bool canRead = false;
            bool bakedUsed = false;

            var mf = root.GetComponent<MeshFilter>();
            if (mf && mf.sharedMesh)
            {
                srcMesh = mf.sharedMesh;
                owner = mf.gameObject;
                toOwnerLocal = owner.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                canRead = srcMesh.isReadable;
            }
            else
            {
                var smr = root.GetComponent<SkinnedMeshRenderer>();
                if (smr)
                {
                    owner = smr.gameObject;
                    var baked = new Mesh();
                    try { smr.BakeMesh(baked, true); }
                    catch { baked = null; }
                    if (baked != null && baked.vertexCount > 0)
                    {
                        srcMesh = baked;
                        bakedUsed = true;
                        toOwnerLocal = owner.transform.worldToLocalMatrix * smr.transform.localToWorldMatrix;
                        canRead = true;
                    }
                }
            }

            if (!srcMesh || srcMesh.vertexCount == 0) yield break;

            int targetLayer = ResolveLayer(layerName);

            // Unreadable MeshFilter path → single convex collider, minimal stall
            if (!canRead)
            {
                yield return BuildSingleConvex(owner, srcMesh, hideOriginalRenderers, targetLayer);
                TryWireLuaBehavior(owner, layerName);
                yield break;
            }

            // Readable path → multi-part
            var verts = new List<Vector3>(srcMesh.vertexCount);
            srcMesh.GetVertices(verts);
            for (int i = 0; i < verts.Count; i++)
            {
                verts[i] = toOwnerLocal.MultiplyPoint3x4(verts[i]);
                if ((i & 255) == 0 && ShouldYield()) { yield return null; }
            }

            var allIdx = new List<int>(65536);
            int smCount = Mathf.Max(1, srcMesh.subMeshCount);
            for (int s = 0; s < smCount; s++)
            {
                var idx = new List<int>(8192);
                try { srcMesh.GetIndices(idx, s); }
                catch { if (s == 0) idx.AddRange(srcMesh.triangles); }
                if (idx.Count >= 3) allIdx.AddRange(idx);
                if (ShouldYield()) { yield return null; }
            }

            if (allIdx.Count < 3)
            {
                yield return BuildSingleConvex(owner, srcMesh, hideOriginalRenderers, targetLayer);
                TryWireLuaBehavior(owner, layerName);
                if (bakedUsed) UnityEngine.Object.Destroy(srcMesh);
                yield break;
            }

            int triCount = allIdx.Count / 3;
            parts = Mathf.Clamp(parts, 1, triCount);

            var triCentroids = new Vector3[triCount];
            for (int t = 0; t < triCount; t++)
            {
                int i0 = allIdx[3 * t + 0];
                int i1 = allIdx[3 * t + 1];
                int i2 = allIdx[3 * t + 2];
                triCentroids[t] = (verts[i0] + verts[i1] + verts[i2]) / 3f;
                if ((t & 255) == 0 && ShouldYield()) { yield return null; }
            }

            var assignments = new int[triCount];
            var centers = new Vector3[parts];

            InitKMeans(triCentroids, centers);
            KMeansAssign(triCentroids, centers, assignments);
            for (int it = 0; it < 8; it++)
            {
                KMeansRecenter(triCentroids, centers, assignments, parts);
                if (ShouldYield()) { yield return null; }
                KMeansAssign(triCentroids, centers, assignments);
                if (ShouldYield()) { yield return null; }
            }

            var holder = FindOrCreateSameNamedChild(owner.transform);
            if (targetLayer != -1) holder.gameObject.layer = targetLayer;

            var perClusterTris = new List<int>[parts];
            for (int k = 0; k < parts; k++) perClusterTris[k] = new List<int>();

            for (int t = 0; t < triCount; t++)
            {
                int k = assignments[t];
                perClusterTris[k].Add(allIdx[3 * t + 0]);
                perClusterTris[k].Add(allIdx[3 * t + 1]);
                perClusterTris[k].Add(allIdx[3 * t + 2]);
                if ((t & 1023) == 0 && ShouldYield()) { yield return null; }
            }

            bool oldSync = Physics.autoSyncTransforms;
            Physics.autoSyncTransforms = false;

            int built = 0;
            for (int k = 0; k < parts; k++)
            {
                var trisK = perClusterTris[k];
                if (trisK.Count < 3) continue;

                var map = new Dictionary<int, int>(trisK.Count);
                var newVerts = new List<Vector3>();
                var newTris = new List<int>(trisK.Count);

                for (int n = 0; n < trisK.Count; n++)
                {
                    int oldIdx = trisK[n];
                    if (!map.TryGetValue(oldIdx, out int newIdx))
                    {
                        newIdx = newVerts.Count;
                        map[oldIdx] = newIdx;
                        newVerts.Add(verts[oldIdx]);
                    }
                    newTris.Add(newIdx);

                    if ((n & 1023) == 0 && ShouldYield()) { yield return null; }
                }

                if (newVerts.Count < 3) continue;

                var go = new GameObject($"Hull_{k}");
                go.transform.SetParent(holder, false);
                if (targetLayer != -1) go.layer = targetLayer;

                var m = new Mesh { name = $"RuntimeHull_{k}" };
                m.SetVertices(newVerts);
                m.SetTriangles(newTris, 0, true);
                m.RecalculateBounds();
                m.UploadMeshData(false);

                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = m;
                mc.convex = true;
                mc.enabled = true;
                mc.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning
                                  | MeshColliderCookingOptions.WeldColocatedVertices
                                  | MeshColliderCookingOptions.CookForFasterSimulation;
                if (!go.GetComponent<GeneratedColliderMarker>()) go.AddComponent<GeneratedColliderMarker>();
                TrySetExcludeLayers(mc, LayerMask.GetMask("GeneratedObject"));

                built++;
                if (built % Mathf.Max(1, batchSize) == 0) { if (ShouldYield()) yield return null; else yield return null; }
            }

            Physics.autoSyncTransforms = oldSync;

            if (hideOriginalRenderers)
            {
                var rens = owner.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rens.Length; i++)
                {
#if UNITY_2021_3_OR_NEWER
                    rens[i].forceRenderingOff = true;
                    if (!rens[i].forceRenderingOff) rens[i].enabled = false;
#else
                    rens[i].enabled = false;
#endif
                    if ((i & 63) == 0 && ShouldYield()) { yield return null; }
                }
            }

            if (wireLuaBehavior) TryWireLuaBehavior(owner, layerName);
            if (bakedUsed) UnityEngine.Object.Destroy(srcMesh);
        }

        static IEnumerator BuildSingleConvex(GameObject owner, Mesh src, bool hide, int targetLayer)
        {
            if (!owner || !src) yield break;

            var mc = owner.GetComponent<MeshCollider>();
            if (!mc) mc = owner.AddComponent<MeshCollider>();
            mc.sharedMesh = src;
            mc.convex = true;
            mc.enabled = true;
            mc.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning
                              | MeshColliderCookingOptions.WeldColocatedVertices
                              | MeshColliderCookingOptions.CookForFasterSimulation;

            if (!owner.GetComponent<GeneratedColliderMarker>()) owner.AddComponent<GeneratedColliderMarker>();
            TrySetExcludeLayers(mc, LayerMask.GetMask("GeneratedObject"));
            if (targetLayer != -1) owner.layer = targetLayer;

            if (hide)
            {
                var rens = owner.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rens.Length; i++)
                {
#if UNITY_2021_3_OR_NEWER
                    rens[i].forceRenderingOff = true;
                    if (!rens[i].forceRenderingOff) rens[i].enabled = false;
#else
                    rens[i].enabled = false;
#endif
                    if ((i & 63) == 0) yield return null;
                }
            }

            yield return null;
        }

        static Transform FindOrCreateSameNamedChild(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c && c.name == parent.name) return c;
            }
            var go = new GameObject(parent.name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static int ResolveLayer(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            int l = LayerMask.NameToLayer(name);
            return l == -1 ? -1 : l;
        }

        static void TryWireLuaBehavior(GameObject owner, string layerName)
        {
            var comps = owner.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i]; if (!c) continue;
                var t = c.GetType(); if (t.Name != "LuaMonoBehavior") continue;
                try
                {
                    var innerField = t.GetField("innerCollider");
                    if (innerField != null) innerField.SetValue(c, null);
                    var acceptField = t.GetField("acceptGeneratedHullCollisions");
                    if (acceptField != null) acceptField.SetValue(c, true);
                    var layerField = t.GetField("generatedLayerName");
                    if (layerField != null) layerField.SetValue(c, layerName);
                } catch {}
                break;
            }
        }

        static void TrySetExcludeLayers(Collider col, int mask)
        {
            var prop = col.GetType().GetProperty("excludeLayers");
            if (prop != null && prop.PropertyType == typeof(int))
            {
                try { prop.SetValue(col, mask, null); } catch {}
            }
        }

        static void InitKMeans(Vector3[] pts, Vector3[] centers)
        {
            int n = pts.Length, k = centers.Length;
            for (int i = 0; i < k; i++) centers[i] = pts[(i * n) / k];
        }

        static void KMeansAssign(Vector3[] pts, Vector3[] centers, int[] assign)
        {
            for (int i = 0; i < pts.Length; i++)
            {
                float best = float.MaxValue; int idx = 0;
                for (int c = 0; c < centers.Length; c++)
                {
                    float d = (pts[i] - centers[c]).sqrMagnitude;
                    if (d < best) { best = d; idx = c; }
                }
                assign[i] = idx;
            }
        }

        static void KMeansRecenter(Vector3[] pts, Vector3[] centers, int[] assign, int k)
        {
            var sums = new Vector3[k];
            var cnts = new int[k];
            for (int i = 0; i < pts.Length; i++)
            {
                int a = assign[i];
                sums[a] += pts[i];
                cnts[a]++;
            }
            for (int c = 0; c < k; c++)
                if (cnts[c] > 0) centers[c] = sums[c] / cnts[c];
        }
    }

    


    

    
public static class VHACDConvexHullColliders
    {
        /// <summary>
        /// Build compound convex MeshColliders from a VHACD hulls prefab.
        /// - Places hulls under a child whose name is EXACTLY the same as the parent (per your requirement).
        /// - Each hull child gets a convex MeshCollider (no mesh read/write needed).
        /// - Optionally hides the original renderers (keeps hulls non-rendered).
        /// - Batches work across frames to avoid stalls/network timeouts.
        /// - Sets LuaMonoBehavior to accept "generated hull" collisions from ANY of these colliders.
        /// </summary>
        public static IEnumerator BuildFromPrefab(
            GameObject root,
            GameObject hullsPrefab,
            bool hideOriginalRenderers = true,
            string layerName = "GeneratedObject",
            int batchSize = 20,
            LuaMonoBehavior parentLua = null)
        {
            const string TAG = "[VHACDConvexHullColliders]";
            if (!root || !hullsPrefab)
            {
                Debug.LogError($"{TAG} root or hullsPrefab is null.");
                yield break;
            }

            if (parentLua == null) parentLua = root.GetComponent<LuaMonoBehavior>();

            // 1) Find or create a child named EXACTLY like the parent
            Transform sameNamedChild = null;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                var c = root.transform.GetChild(i);
                if (c && c.name == root.name) { sameNamedChild = c; break; }
            }
            if (!sameNamedChild)
            {
                var go = new GameObject(root.name);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                sameNamedChild = go.transform;
            }

            // 2) Instantiate the hulls prefab under this child (identity transform)
            var hullsRoot = UnityEngine.Object.Instantiate(hullsPrefab, sameNamedChild, false);
            hullsRoot.name = "ConvexHulls";

            // Optional: set layer on the whole hulls tree
            int targetLayer = -1;
            if (!string.IsNullOrEmpty(layerName))
            {
                int l = LayerMask.NameToLayer(layerName);
                if (l != -1) targetLayer = l;
            }
            if (targetLayer != -1) SetLayerRecursively(hullsRoot, targetLayer);

            // 3) For each hull child with a MeshFilter, add a convex MeshCollider (disable any renderer)
            var filters = hullsRoot.GetComponentsInChildren<MeshFilter>(includeInactive: true);
            int created = 0;
            foreach (var mf in filters)
            {
                if (!mf || !mf.sharedMesh) continue;

                var go = mf.gameObject;

                // Turn off visuals if present (keep GO active)
                var r = go.GetComponent<Renderer>();
                if (r)
                {
#if UNITY_2021_3_OR_NEWER
                    r.forceRenderingOff = true;
                    if (!r.forceRenderingOff) r.enabled = false;
#else
                r.enabled = false;
#endif
                }

                // Add or reuse MeshCollider
                var mc = go.GetComponent<MeshCollider>();
                if (!mc) mc = go.AddComponent<MeshCollider>();

                mc.sharedMesh = mf.sharedMesh; // convex hull mesh from VHACD
                mc.convex = true;              // required for dynamic rigidbodies
                mc.enabled = true;

                // Helpful cooking options (safe defaults)
                mc.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning
                                  | MeshColliderCookingOptions.WeldColocatedVertices
                                  | MeshColliderCookingOptions.CookForFasterSimulation;

                // Mark as "generated" so we can filter later without relying solely on layers
                if (!go.GetComponent<GeneratedColliderMarker>()) go.AddComponent<GeneratedColliderMarker>();

                // Try to set custom 'excludeLayers' if your project exposes it on colliders
                TrySetExcludeLayers(mc, LayerMask.GetMask("GeneratedObject"));

                created++;
                if (created % batchSize == 0) yield return null;
            }

            // 4) Optionally hide the original renderers (skip anything under the hulls tree)
            if (hideOriginalRenderers)
            {
                var allRenderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
                int disabled = 0;
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    var r = allRenderers[i];
                    if (!r) continue;
                    if (r.transform.IsChildOf(hullsRoot.transform)) continue; // keep hulls hidden already

#if UNITY_2021_3_OR_NEWER
                    r.forceRenderingOff = true;
                    if (!r.forceRenderingOff) r.enabled = false;
#else
                r.enabled = false;
#endif
                    disabled++;
                    if ((i + 1) % batchSize == 0) yield return null;
                }
                Debug.Log($"{TAG} '{root.name}': hull colliders={created}, original renderers disabled={disabled}");
            }
            else
            {
                Debug.Log($"{TAG} '{root.name}': hull colliders={created}, original renderers kept visible");
            }

            // 5) Tell LuaMonoBehavior to accept collisions from ANY generated hull collider
            if (parentLua != null)
            {
                // We’re using the marker/layer filter instead of a single innerCollider
                parentLua.innerCollider = null;
                parentLua.acceptGeneratedHullCollisions = true;   // (you’ll add this field below)
                parentLua.generatedLayerName = layerName;         // (field below)
            }
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform t in go.transform) SetLayerRecursively(t.gameObject, layer);
        }

        private static void TrySetExcludeLayers(Collider col, int mask)
        {
            var prop = col.GetType().GetProperty("excludeLayers");
            if (prop != null && prop.PropertyType == typeof(int))
            {
                try { prop.SetValue(col, mask); } catch { /* ignore */ }
            }
        }
    }


    public class GeneratedColliderMarker : MonoBehaviour { }


    public static class CompoundColliderBuilder
    {
        /// <param name="root">The object to build colliders for (parent should own the Rigidbody).</param>
        /// <param name="hideRenderers">Hide all renderers so only colliders remain active.</param>
        /// <param name="layerName">Layer to put generated colliders on (e.g. "GeneratedObject"). Use null/"" to leave unchanged.</param>
        /// <param name="batchSize">Work spread across frames to avoid stalls/network timeouts.</param>
        /// <param name="alsoAddSingleBoundsChild">
        /// If true, also creates a child named EXACTLY like the parent and places one big bounds BoxCollider on it,
        /// then wires LuaMonoBehavior.innerCollider to that BoxCollider.
        /// </param>
        /// <param name="parentLuaBehavior">
        /// Optional: pass the LuaMonoBehavior on the parent so we can assign innerCollider automatically.
        /// If null, we’ll try GetComponent&lt;LuaMonoBehavior&gt;() on root.
        /// </param>
        public static IEnumerator Build(
            GameObject root,
            bool hideRenderers = true,
            string layerName = "GeneratedObject",
            int batchSize = 30,
            bool alsoAddSingleBoundsChild = true,
            LuaMonoBehavior parentLuaBehavior = null)
        {
            const string TAG = "[CompoundColliderBuilder]";
            if (!root) yield break;

            if (parentLuaBehavior == null)
                parentLuaBehavior = root.GetComponent<LuaMonoBehavior>();

            // 0) Resolve layer target (optional)
            int targetLayer = -1;
            if (!string.IsNullOrEmpty(layerName))
            {
                int l = LayerMask.NameToLayer(layerName);
                if (l != -1) targetLayer = l;
            }

            // 1) Per-renderer BoxColliders (cheap + robust)
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            int made = 0, disabled = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (!r) continue;

                var t = r.transform;
                var bc = t.gameObject.GetComponent<BoxCollider>();
                if (!bc) bc = t.gameObject.AddComponent<BoxCollider>();

                // mark & layer
                if (!t.gameObject.GetComponent<GeneratedColliderMarker>())
                    t.gameObject.AddComponent<GeneratedColliderMarker>();
                if (targetLayer != -1)
                    t.gameObject.layer = targetLayer;

                // world -> local
                var wb = r.bounds; // world
                var centerLocal = t.InverseTransformPoint(wb.center);
                var ls = t.lossyScale;
                Vector3 safe = new Vector3(
                    Mathf.Approximately(ls.x, 0f) ? 1f : Mathf.Abs(ls.x),
                    Mathf.Approximately(ls.y, 0f) ? 1f : Mathf.Abs(ls.y),
                    Mathf.Approximately(ls.z, 0f) ? 1f : Mathf.Abs(ls.z)
                );

                bc.center = centerLocal;
                bc.size = new Vector3(
                    wb.size.x / safe.x,
                    wb.size.y / safe.y,
                    wb.size.z / safe.z
                );

                // Optionally hide the renderer (keep the GO active for the collider!)
#if UNITY_2021_3_OR_NEWER
                if (hideRenderers) { r.forceRenderingOff = true; if (!r.forceRenderingOff) r.enabled = false; disabled++; }
#else
            if (hideRenderers) { r.enabled = false; disabled++; }
#endif

                made++;
                if (made % batchSize == 0)
                    yield return null; // spread work across frames
            }

            // 2) Optional: one big bounds BoxCollider on a child named EXACTLY like the parent
            Collider inner = null;
            if (alsoAddSingleBoundsChild)
            {
                if (renderers.Length > 0)
                {
                    bool init = false;
                    Bounds wb = default;
                    foreach (var r in renderers)
                    {
                        if (!r) continue;
                        if (!init) { wb = r.bounds; init = true; }
                        else wb.Encapsulate(r.bounds);
                    }

                    if (init)
                    {
                        // find/create same-named child
                        Transform child = null;
                        for (int i = 0; i < root.transform.childCount; i++)
                        {
                            var c = root.transform.GetChild(i);
                            if (c && c.name == root.name) { child = c; break; }
                        }
                        if (!child)
                        {
                            var go = new GameObject(root.name);
                            go.transform.SetParent(root.transform, false);
                            go.transform.localPosition = Vector3.zero;
                            go.transform.localRotation = Quaternion.identity;
                            go.transform.localScale = Vector3.one;
                            child = go.transform;
                        }

                        if (targetLayer != -1) child.gameObject.layer = targetLayer;

                        var bc = child.GetComponent<BoxCollider>();
                        if (!bc) bc = child.gameObject.AddComponent<BoxCollider>();
                        if (!child.GetComponent<GeneratedColliderMarker>())
                            child.gameObject.AddComponent<GeneratedColliderMarker>();

                        // Fit overall bounds
                        bc.center = child.InverseTransformPoint(wb.center);
                        var ls = child.lossyScale;
                        Vector3 safe = new Vector3(
                            Mathf.Approximately(ls.x, 0f) ? 1f : Mathf.Abs(ls.x),
                            Mathf.Approximately(ls.y, 0f) ? 1f : Mathf.Abs(ls.y),
                            Mathf.Approximately(ls.z, 0f) ? 1f : Mathf.Abs(ls.z)
                        );
                        bc.size = new Vector3(
                            wb.size.x / safe.x,
                            wb.size.y / safe.y,
                            wb.size.z / safe.z
                        );

                        inner = bc;
                        yield return null;
                    }
                }
            }

            // 3) Wire LuaMonoBehavior.innerCollider (if available)
            if (parentLuaBehavior != null)
            {
                if (inner == null)
                {
                    // fallback: pick the first generated BoxCollider under root
                    var any = root.GetComponentInChildren<BoxCollider>(includeInactive: true);
                    if (any != null) inner = any;
                }
                parentLuaBehavior.innerCollider = inner;
            }

            // 4) Optional: wire InnerCompoundRouter if present on the parent
            var router = root.GetComponent<InnerCompoundRouter>();
            if (router != null)
            {
                router.inner = inner; // can be null (router supports fallback filtering)
            }

            Debug.Log($"{TAG} '{root.name}': generated {made} box colliders, disabled {disabled} renderers, inner={(inner ? inner.name : "null")}");
        }
    }



    public class ModelIformation
    {
        public GameObject gameobjectWarp;
        public string ModelURL;

        public MeshType meshType;


        public MeshType modelType()
        {

            if (ModelURL.Contains("scaned"))
            {
                if (ModelURL.Contains("target"))
                {

                    meshType = MeshType.TargetObject;
                }

                if (ModelURL.Contains("background"))
                {


                    meshType = MeshType.BackGround;

                }

                if (ModelURL.Contains("Instruction"))
                {

                    meshType = MeshType.Modified;

                }

            }
            else if (ModelURL.Contains("generated"))
            {


                meshType = MeshType.Generated;


            }


            return meshType;
        }





    }

    public class WebSocketmessage
    {
        public string ID;
        public string prompt;

        public string getMsg()
        {

            return "" + ID + "," + prompt;

        }

        public void setMsg(string msg)
        {
            string[] data = msg.Split(",");

            ID = data[0];
            prompt = data[1];


        }




    }





    public static class TimestampGenerator
    {
        public static string GetTimestamp()
        {
            // Get the current date and time
            DateTime now = DateTime.Now;

            // Format the date and time as a timestamp string
            string timestamp = now.ToString("yyyyMMddHHmmss");

            return timestamp;
        }
    }


    public static class IDGenerator
{
    public static string GenerateID()
    {
        // Get the current date and time
        DateTime now = DateTime.Now;

        // Format the date and time as a timestamp string
        string timestamp = now.ToString("yyyyMMddHHmmss");

        // Append a unique identifier
        string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8); // Shortened Guid for readability

        return $"{timestamp}{uniqueId}";
    }
}




    public static class URLChecker
    {
        public static bool CheckURLConnection(string url)
        {
            try
            {
                // Create a web request to the specified URL
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                // Set the request method to HEAD to only get headers without downloading the content
                request.Method = "HEAD";

                // Get the response
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Check if the response status is OK (200)
                bool connectionExists = response.StatusCode == HttpStatusCode.OK;

                // Close the response
                response.Close();

                return connectionExists;
            }
            catch (Exception e)
            {
                // An exception occurred, indicating that the URL connection does not exist
                //  Debug.LogError("Error checking URL connection: " + e.Message);
                return false;
            }
        }
    }



    

public class ColliderUtils : MonoBehaviour
{
    /// <summary>
    /// Adds a MeshCollider to the first MeshFilter found under the given GameObject.
    /// </summary>
    /// <param name="parentObject">The GameObject to search (e.g., "TargetObject").</param>
    /// <param name="convex">Whether the collider should be convex (true) or not (false).</param>
    /// <returns>The MeshCollider that was added (or already existed), or null if none found.</returns>
    public static MeshCollider AddMeshCollider(GameObject parentObject, bool convex = true)
    {
        if (parentObject == null)
        {
            Debug.LogWarning("AddMeshCollider: parentObject is null.");
            return null;
        }

        // Look for a MeshFilter on the parent or its children
        MeshFilter meshFilter = parentObject.GetComponentInChildren<MeshFilter>(includeInactive: true);
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning($"AddMeshCollider: No MeshFilter with mesh found under \"{parentObject.name}\".");
            return null;
        }

        // Get or add MeshCollider to the object with the mesh
        GameObject target = meshFilter.gameObject;
        MeshCollider meshCollider = target.GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = target.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = convex;

        return meshCollider;
    }

    }
}








