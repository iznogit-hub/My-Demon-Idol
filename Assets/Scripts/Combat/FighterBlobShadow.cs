using UnityEngine;

namespace DemonIdol.Combat
{
    /// <summary>
    /// Soft grounded blob/contact shadow for 2.5D fighting game characters.
    /// Anchors the fighter visually to the arena floor at Y = 0.01m.
    /// Modulates scale and alpha dynamically when the fighter jumps airborne.
    /// </summary>
    [ExecuteAlways]
    public class FighterBlobShadow : MonoBehaviour
    {
        [Header("Target Fighter")]
        public Transform fighterTarget;

        [Header("Shadow Dimensions")]
        public float baseRadiusX = 0.95f;
        public float baseRadiusZ = 0.45f;
        public float groundY = 0.04f;

        [Header("Airborne Attenuation")]
        public float maxJumpHeight = 3.5f;
        public float minScaleAtPeak = 0.55f;
        [Range(0f, 1f)]
        public float baseAlpha = 0.65f;
        [Range(0f, 1f)]
        public float minAlphaAtPeak = 0.22f;

        private GameObject _shadowQuad;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _mpb;
        private Material _shadowMaterial;
        private static Texture2D _blobTexture;

        private static readonly int ColorPropId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorLegacyPropId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (fighterTarget == null)
            {
                fighterTarget = transform;
            }
            EnsureShadowObject();
        }

        private void OnEnable()
        {
            EnsureShadowObject();
            if (_shadowQuad != null) _shadowQuad.SetActive(true);
        }

        private void OnDisable()
        {
            if (_shadowQuad != null) _shadowQuad.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_shadowQuad != null)
            {
                if (Application.isPlaying) Destroy(_shadowQuad);
                else DestroyImmediate(_shadowQuad);
            }
        }

        public static Texture2D GetOrCreateBlobTexture()
        {
            if (_blobTexture != null) return _blobTexture;

            const int size = 128;
            _blobTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _blobTexture.name = "SoftBlobShadow_Radial";
            _blobTexture.wrapMode = TextureWrapMode.Clamp;
            _blobTexture.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float distSq = dx * dx + dy * dy;

                    float alpha = 0f;
                    if (distSq < 0.85f)
                    {
                        float dist = Mathf.Sqrt(distSq) / Mathf.Sqrt(0.85f);
                        alpha = Mathf.SmoothStep(1.0f, 0.0f, dist);
                        alpha = alpha * alpha; // Soft falloff
                    }

                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            _blobTexture.SetPixels(colors);
            _blobTexture.Apply();
            return _blobTexture;
        }

        public void EnsureShadowObject()
        {
            if (_shadowQuad != null) return;

            // Look for existing child
            Transform existing = transform.Find("Contact_BlobShadow");
            if (existing != null)
            {
                _shadowQuad = existing.gameObject;
            }
            else
            {
                _shadowQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _shadowQuad.name = "Contact_BlobShadow";
                // Remove collider
                var col = _shadowQuad.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying) Destroy(col);
                    else DestroyImmediate(col);
                }
            }

            _shadowQuad.transform.SetParent(transform, false);
            _shadowQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _renderer = _shadowQuad.GetComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            // Prefer URP Unlit transparent shader
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Sprites/Default");

            if (shader != null)
            {
                _shadowMaterial = new Material(shader);
                _shadowMaterial.name = "M_ContactBlobShadow_Instance";
                Texture2D tex = GetOrCreateBlobTexture();
                _shadowMaterial.mainTexture = tex;

                if (_shadowMaterial.HasProperty("_BaseMap"))
                    _shadowMaterial.SetTexture("_BaseMap", tex);
                if (_shadowMaterial.HasProperty("_MainTex"))
                    _shadowMaterial.SetTexture("_MainTex", tex);

                // Set transparent surface mode for URP
                if (_shadowMaterial.HasProperty("_Surface"))
                    _shadowMaterial.SetFloat("_Surface", 1f); // Transparent
                if (_shadowMaterial.HasProperty("_Blend"))
                    _shadowMaterial.SetFloat("_Blend", 0f); // Alpha blend

                _shadowMaterial.SetOverrideTag("RenderType", "Transparent");
                _shadowMaterial.renderQueue = 3000;
                _shadowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _shadowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _shadowMaterial.SetInt("_ZWrite", 0);
                _shadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _shadowMaterial.EnableKeyword("_ALPHABLEND_ON");

                _renderer.sharedMaterial = _shadowMaterial;
            }

            _mpb = new MaterialPropertyBlock();
        }

        public void UpdateShadowTransform(Vector3 targetPos)
        {
            if (_shadowQuad == null) EnsureShadowObject();
            if (_shadowQuad == null) return;

            float airborneY = Mathf.Max(0f, targetPos.y);
            float jumpT = Mathf.Clamp01(airborneY / maxJumpHeight);

            float currentScale = Mathf.Lerp(1.0f, minScaleAtPeak, jumpT);
            float currentAlpha = Mathf.Lerp(baseAlpha, minAlphaAtPeak, jumpT);

            _shadowQuad.transform.position = new Vector3(targetPos.x, groundY, targetPos.z);
            _shadowQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            Vector3 parentScale = transform.lossyScale;
            float invSx = Mathf.Abs(parentScale.x) > 0.001f ? (1f / Mathf.Abs(parentScale.x)) : 1f;
            float invSz = Mathf.Abs(parentScale.z) > 0.001f ? (1f / Mathf.Abs(parentScale.z)) : 1f;
            _shadowQuad.transform.localScale = new Vector3(baseRadiusX * 2f * currentScale * invSx, baseRadiusZ * 2f * currentScale * invSz, 1f);

            if (_renderer != null)
            {
                if (_mpb == null) _mpb = new MaterialPropertyBlock();
                Color shadowColor = new Color(0.04f, 0.05f, 0.08f, currentAlpha);
                _renderer.GetPropertyBlock(_mpb);
                _mpb.SetColor(ColorPropId, shadowColor);
                _mpb.SetColor(ColorLegacyPropId, shadowColor);
                _renderer.SetPropertyBlock(_mpb);
            }
        }

        public static Vector3 GetFighterGroundCenter(Transform target)
        {
            if (target == null) return Vector3.zero;
            var anim = target.GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                {
                    return new Vector3(hips.position.x, target.position.y, hips.position.z);
                }
            }
            Transform hipTransform = target.Find("Hips") 
                ?? target.Find("Armature/Hips") 
                ?? target.Find("root/pelvis")
                ?? target.Find("Root/Hip");
            if (hipTransform != null)
            {
                return new Vector3(hipTransform.position.x, target.position.y, hipTransform.position.z);
            }
            return target.position;
        }

        private void LateUpdate()
        {
            if (fighterTarget == null) fighterTarget = transform;
            UpdateShadowTransform(GetFighterGroundCenter(fighterTarget));
        }
    }
}
