using UnityEngine;

namespace SkiStation.Arcade
{
    // Standalone test slope; no demo resort, no visitor spawn and no management scripts.
    public sealed class ArcadeTestSlope : MonoBehaviour
    {
        public ArcadeSkiController skier;
        public Camera skiCamera;
        ArcadeThirdPersonCamera followCamera;
        Vector3 startPosition;
        Quaternion startRotation;
        ParticleSystem snow;
        float snowBurstUntil;
        string status = "";
        Material snowMaterial, rockMaterial, treeMaterial;
        Material particleMaterial, trackMaterial;
        Texture2D snowTexture;

        void Start()
        {
            snowMaterial = MakeMaterial(new Color(0.89f, 0.96f, 1f));
            rockMaterial = MakeMaterial(new Color(0.39f, 0.43f, 0.47f));
            treeMaterial = MakeMaterial(new Color(0.08f, 0.30f, 0.22f));
            BuildSnowMaterials();
            BuildTerrain(); BuildObstacles(); BuildPlayer(); BuildSnow();
            ArcadePlaytestDiagnostics diagnostics = gameObject.AddComponent<ArcadePlaytestDiagnostics>();
            diagnostics.skier = skier; diagnostics.followCamera = skiCamera;
            gameObject.AddComponent<ArcadePlaytestReadiness>();
            RenderSettings.fog = false;
            RenderSettings.ambientLight = new Color(0.75f, 0.80f, 0.9f);
            GameObject sun = new GameObject("Arcade test sun");
            Light light = sun.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
        }
        Material MakeMaterial(Color color)
        {
            Shader shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            Material material = new Material(shader); material.color = color; return material;
        }
        void BuildSnowMaterials()
        {
            bool srp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            Shader shader = Shader.Find(srp ? "Universal Render Pipeline/Particles/Unlit" : "Sprites/Default");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[Arcade 2.41.3] Shader neige indisponible : effets desactives.");
                return;
            }
            particleMaterial = new Material(shader);
            trackMaterial = new Material(shader);
            // Explicit alpha blending and vertex colour preserve snow/boost tint.
            foreach (Material material in new Material[] { particleMaterial, trackMaterial })
            {
                if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                if (srp) material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000;
            }
            snowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            snowTexture.wrapMode = TextureWrapMode.Clamp;
            for (int y=0; y<32; y++) for (int x=0; x<32; x++)
            {
                float radius = new Vector2((x-15.5f)/15.5f, (y-15.5f)/15.5f).magnitude;
                snowTexture.SetPixel(x,y,new Color(1f,1f,1f,1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.35f,1f,radius))));
            }
            snowTexture.Apply(false, true);
            if (particleMaterial.HasProperty("_MainTex")) particleMaterial.SetTexture("_MainTex", snowTexture);
            if (particleMaterial.HasProperty("_BaseMap")) particleMaterial.SetTexture("_BaseMap", snowTexture);
        }
        void ConfigureSnowRenderer(ParticleSystem system)
        {
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.enabled = particleMaterial != null;
            if (particleMaterial != null) renderer.sharedMaterial = particleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        void OnDestroy()
        {
            if (particleMaterial != null) Destroy(particleMaterial);
            if (trackMaterial != null) Destroy(trackMaterial);
            if (snowTexture != null) Destroy(snowTexture);
        }
        float Height(float x, float z)
        {
            // Broad, continuous snow rollers: no cube wall at the ramp entrance.
            float lane = Mathf.Exp(-x * x / 180f);
            float roller = 4f * Mathf.Exp(-(z - 52f) * (z - 52f) / 100f)
                + 6f * Mathf.Exp(-(z - 150f) * (z - 150f) / 196f);
            return 75f - z * 0.25f + Mathf.Sin(x * 0.085f) * 2.0f
                + Mathf.Sin(z * 0.11f + x * 0.045f) * 1.1f + lane * roller;
        }
        void BuildTerrain()
        {
            const int nx = 80, nz = 130; const float step = 3f;
            Vector3[] vertices = new Vector3[(nx + 1) * (nz + 1)];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[nx * nz * 6];
            for (int z = 0; z <= nz; z++) for (int x = 0; x <= nx; x++)
            {
                int index = z * (nx + 1) + x;
                float wx = (x - nx / 2f) * step, wz = (z - 8) * step;
                vertices[index] = new Vector3(wx, Height(wx, wz), wz);
                uv[index] = new Vector2(x / 12f, z / 12f);
            }
            int t = 0;
            for (int z = 0; z < nz; z++) for (int x = 0; x < nx; x++)
            {
                int a = z * (nx + 1) + x, b = a + 1, c = a + nx + 1, d = c + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }
            Mesh mesh = new Mesh(); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; mesh.RecalculateNormals();
            GameObject terrain = new GameObject("Arcade snow terrain (mesh + collider)");
            terrain.transform.SetParent(transform);
            terrain.AddComponent<MeshFilter>().sharedMesh = mesh;
            terrain.AddComponent<MeshRenderer>().sharedMaterial = snowMaterial;
            terrain.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        void BuildObstacles()
        {
            for (int i = 0; i < 28; i++)
            {
                float x = Mathf.Sin(i * 7.13f) * 90f;
                float z = 28f + i * 10f;
                if (Mathf.Abs(x) < 13f) x += x < 0 ? -16f : 16f;
                float y = Height(x, z);
                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Deflecting tree " + i; trunk.transform.SetParent(transform);
                trunk.transform.position = new Vector3(x, y + 2f, z);
                trunk.transform.localScale = new Vector3(0.65f, 2f, 0.65f);
                trunk.GetComponent<Renderer>().sharedMaterial = rockMaterial;
                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                crown.name = "Tree crown"; crown.transform.SetParent(trunk.transform);
                crown.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                crown.transform.localScale = new Vector3(3f, 1.5f, 3f);
                crown.GetComponent<Renderer>().sharedMaterial = treeMaterial;
            }
            for (int i = 0; i < 5; i++)
            {
                float x = i % 2 == 0 ? -35f : 38f, z = 55f + i * 49f;
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Deflecting rock " + i; rock.transform.SetParent(transform);
                rock.transform.position = new Vector3(x, Height(x, z) + 2f, z);
                rock.transform.localScale = new Vector3(7f, 4f, 6f);
                rock.transform.rotation = Quaternion.Euler(0f, i * 22f, 12f);
                rock.GetComponent<Renderer>().sharedMaterial = rockMaterial;
            }
            // Mark the two jump approaches without adding collision geometry.
            foreach (float z in new float[] { 42f, 136f })
                foreach (float x in new float[] { -8f, 8f })
                {
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = "Jump approach marker";
                    marker.transform.SetParent(transform);
                    marker.transform.position = new Vector3(x, Height(x,z)+1.2f,z);
                    marker.transform.localScale = new Vector3(.16f,1.2f,.16f);
                    marker.GetComponent<Renderer>().sharedMaterial = rockMaterial;
                    marker.GetComponent<Collider>().enabled = false;
                }
        }
        void BuildPlayer()
        {
            GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            avatar.name = "PLAYER - Arcade skier (test avatar)"; avatar.transform.SetParent(transform);
            avatar.GetComponent<Renderer>().sharedMaterial = MakeMaterial(new Color(0.95f, 0.28f, 0.26f));
            Collider collider = avatar.GetComponent<Collider>(); collider.isTrigger = true;
            skier = avatar.AddComponent<ArcadeSkiController>();
            ArcadeBigAirLanding bigAir = avatar.AddComponent<ArcadeBigAirLanding>();
            bigAir.skier = skier;
            ArcadeComboHUD comboHUD = avatar.AddComponent<ArcadeComboHUD>();
            comboHUD.skier = skier;
            ArcadeTrickNameHUD trickNames = avatar.AddComponent<ArcadeTrickNameHUD>();
            avatar.AddComponent<ArcadeLandingCelebrationHUD>();
            trickNames.skier = skier;

            startPosition = new Vector3(0f, Height(0f, 0f) + 0.12f, 0f);
            startRotation = Quaternion.identity; skier.BeginAt(startPosition, startRotation);
            skier.Landed += OnLanding; skier.Crashed += OnCrash; skier.Retried += OnRetry;
            // The entire player avatar remains visible in the third-person follow view.
            ArcadeSkierRig.Build(avatar.transform, skier,
                MakeMaterial(new Color(.95f,.28f,.26f)), rockMaterial, snowMaterial);
            ArcadeSkiTrickVisual trickVisual = avatar.AddComponent<ArcadeSkiTrickVisual>();
            trickVisual.controller = skier;
            trickVisual.bodyVisual = avatar.transform.Find("Arcade body visual");
            trickVisual.leftSki = trickVisual.bodyVisual.Find("Left ski");
            trickVisual.rightSki = trickVisual.bodyVisual.Find("Right ski");
            ArcadeCarvingVisual carving = avatar.AddComponent<ArcadeCarvingVisual>();
            carving.controller = skier;
            carving.body = trickVisual.bodyVisual;
            carving.leftSki = trickVisual.leftSki;
            carving.rightSki = trickVisual.rightSki;
            carving.leftTrack = CreateSkiTrack(trickVisual.leftSki, "Left snow track");
            carving.rightTrack = CreateSkiTrack(trickVisual.rightSki, "Right snow track");
            carving.carvingSnow = CreateCarvingSnow(avatar.transform);
            GameObject cameraObject = new GameObject("Third-person arcade follow camera");
            skiCamera = cameraObject.AddComponent<Camera>(); skiCamera.fieldOfView = 72f;
            followCamera = cameraObject.AddComponent<ArcadeThirdPersonCamera>();
            followCamera.target = avatar.transform;
            followCamera.controller = skier;
            followCamera.SnapBehind();
            ArcadePeripheralSpeedEffect speedEffects = cameraObject.AddComponent<ArcadePeripheralSpeedEffect>();
            speedEffects.controller = skier;
            // Start with inexpensive feedback; full-screen blur can be enabled later.
            speedEffects.showBlurWhenSupported = false;
            skiCamera.nearClipPlane = 0.08f;
            cameraObject.AddComponent<AudioListener>();
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera other in cameras) if (other != skiCamera) other.enabled = false;
            foreach (AudioListener listener in FindObjectsOfType<AudioListener>())
                if (listener.gameObject != cameraObject) listener.enabled = false;
            avatar.GetComponent<Renderer>().enabled = false;
        }
        TrailRenderer CreateSkiTrack(Transform ski, string name)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(ski, false);
            marker.transform.localPosition = new Vector3(0f, -0.07f, -0.45f);
            TrailRenderer track = marker.AddComponent<TrailRenderer>();
            track.time = 5f;
            track.startWidth = 0.11f;
            track.endWidth = 0.025f;
            track.minVertexDistance = 0.12f;
            track.startColor = new Color(0.57f, 0.73f, 0.83f, 0.8f);
            track.endColor = new Color(0.57f, 0.73f, 0.83f, 0f);
            track.sharedMaterial = trackMaterial;
            track.enabled = trackMaterial != null;
            track.emitting = false;
            return track;
        }
        ParticleSystem CreateCarvingSnow(Transform avatar)
        {
            GameObject effect = new GameObject("Carving powder spray");
            effect.transform.SetParent(avatar, false);
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            ConfigureSnowRenderer(particles);
            var main = particles.main;
            main.startLifetime = 0.45f;
            main.startSpeed = 2.8f;
            main.startSize = 0.15f;
            main.startColor = Color.white;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.35f;
            return particles;
        }
        void BuildSnow()
        {
            GameObject effect = new GameObject("Landing snow burst"); effect.transform.SetParent(transform);
            snow = effect.AddComponent<ParticleSystem>();
            ConfigureSnowRenderer(snow);
            var main = snow.main; main.startLifetime = 0.75f; main.startSpeed = 7f;
            main.startSize = 0.32f; main.startColor = Color.white;
            main.maxParticles = 600; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = snow.emission; emission.enabled = false;
            var shape = snow.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 1f;
            snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnLanding(float impact)
        {
            if (snow != null)
            {
                snow.transform.position = skier.transform.position;
                var main = snow.main; main.startSpeed = Mathf.Clamp(impact * 0.38f, 4f, 28f);
                if (skier.IsUnlimitedBoosting)
                {
                    main.startColor = new ParticleSystem.MinMaxGradient(
                        new Color(1f, 0.05f, 0.04f), new Color(1f, 0.45f, 0.02f));
                    main.startSize = 0.52f;
                    snow.Emit(Mathf.RoundToInt(Mathf.Clamp(impact * 6f, 28f, 430f)));
                }
                else
                {
                    main.startColor = skier.IsRegularBoosting
                        ? new Color(0.05f, 1f, 0.85f) : Color.white;
                    main.startSize = 0.32f;
                    snow.Emit(Mathf.RoundToInt(Mathf.Clamp(impact * 3.5f, 12f, 230f)));
                }
            }
            status = "RECEPTION ! Impact " + impact.ToString("0") + " - aucune chute liee a la hauteur";
            snowBurstUntil = Time.time + 2f;
        }
        void OnCrash() { status = "RECEPTION DE TRAVERS (" + skier.LastLandingAngleError.ToString("0") + " deg) - R pour recommencer"; }
        void OnRetry() { status = "Nouvelle tentative !"; snowBurstUntil = Time.time + 2f; }
        void Update()
        {
            if (skier == null) return;
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                skier.BeginAt(startPosition, startRotation);
                ArcadeSkiTrickVisual visual = skier.GetComponent<ArcadeSkiTrickVisual>();
                if (visual != null) visual.ResetVisual();
            }
            if (skier.transform.position.y < -100f || skier.transform.position.z > 350f)
            {
                skier.BeginAt(startPosition, startRotation);
                ArcadeSkiTrickVisual visual = skier.GetComponent<ArcadeSkiTrickVisual>();
                if (visual != null) visual.ResetVisual();
            }

        }
        void OnGUI()
        {
            if (skier != null)
            {
                float width = Mathf.Min(340f, Screen.width * 0.32f);
                Rect bar = new Rect(Screen.width - width - 18f, 20f, width, 30f);
                GUI.Box(bar, "");
                GUI.color = skier.IsUnlimitedBoosting ? new Color(1f, 0.07f, 0.08f) : skier.IsRegularBoosting ? new Color(0.05f, 1f, 0.75f) : new Color(0.25f, 0.85f, 1f);
                GUI.DrawTexture(new Rect(bar.x + 3f, bar.y + 3f, (width - 6f) * skier.BoostFraction, 24f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(bar.x + 8f, bar.y + 5f, width - 16f, 23f),
                    skier.UnlimitedBoostUnlocked ? "BOOST ILLIMITE  " + skier.unlimitedBoostTimeRemaining.ToString("0.0") + " s [MAJ]" : "BOOST  " + Mathf.RoundToInt(skier.BoostFraction * 100f) + "% [MAJ]");
            }
            ArcadeBigAirLanding bigAirSetting = skier.GetComponent<ArcadeBigAirLanding>();
            if (bigAirSetting != null)
            {
                bool choice = GUI.Toggle(new Rect(14, 165, 460, 28), bigAirSetting.SlowMotionEnabled,
                    "Ralenti bref des receptions Big Air (parametre sauvegarde)");
                if (choice != bigAirSetting.SlowMotionEnabled) bigAirSetting.SetSlowMotionEnabled(choice);
            }
            GUI.Box(new Rect(12, 12, 510, 145), "SKISTATION - ARCADE 2.42 - GLISSE ET RECEPTIONS");
            GUI.Label(new Rect(26, 42, 480, 24), "Pente : vitesse automatique | Fleches G/D : tourner | Ctrl/S : frein");
            GUI.Label(new Rect(26, 65, 480, 24), "F/G : rotations 180/360 | MAJ : boost | Reception face/dos +/-20 deg");
            GUI.Label(new Rect(26, 88, 480, 24), "Espace : maintenir puis relacher | H : grab | Retour : depart");
            GUI.Label(new Rect(26, 112, 480, 24), (skier.UnlimitedBoostUnlocked ? "BOOST ILLIMITE : " + skier.unlimitedBoostTimeRemaining.ToString("0.0") + " s" : "BOOST : " + Mathf.RoundToInt(skier.BoostFraction * 100f) + "%") + "  |  " +
                (skier.IsBoosting ? "BOOST ACTIF !  |  " : "") + "Vitesse : " + skier.Speed.ToString("0.0") + " m/s  |  " +
                (skier.IsCrashed ? "CHUTE" : skier.IsAirborne ? "EN L'AIR" : skier.IsChargingJump
                    ? "SAUT CHARGE " + Mathf.RoundToInt(skier.JumpCharge01 * 100f) + "%" : skier.IsSwitch ? "SWITCH (DOS)" : "SUR LA NEIGE"));
            if (skier.IsCrashed)
            {
                GUI.Box(new Rect(Screen.width / 2f - 220f, Screen.height / 2f - 70f, 440f, 140f),
                    "FIGURE RATEE !\nTu peux regarder la chute ou recommencer.");
                if (GUI.Button(new Rect(Screen.width / 2f - 110f, Screen.height / 2f + 5f, 220f, 42f),
                    "RECOMMENCER (R)")) skier.Retry();
            }
            if (skier.IsCrashed || Time.time < snowBurstUntil)
                GUI.Box(new Rect(12, Screen.height - 70, 510, 50), status);
        }
    }
}
