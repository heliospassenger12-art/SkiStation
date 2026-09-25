using UnityEngine;

namespace SkiStation
{
    /// <summary>Decorates the existing arrival hub; never changes routes, slots or admissions.</summary>
    public sealed class ArrivalWelcomeVisuals256 : MonoBehaviour
    {
        private ArrivalHub250 hub;
        private TextMesh carCount, busCount;
        private float refreshAt;
        private Material wood, darkWood, snow, metal, glass, blue, white;

        public static void Install(ArrivalHub250 hub)
        {
            if (hub == null || hub.Office == null || hub.GetComponentInChildren<ArrivalWelcomeVisuals256>() != null) return;
            var root = new GameObject("Accueil_visuel_256");
            root.transform.SetParent(hub.transform, false);
            root.AddComponent<ArrivalWelcomeVisuals256>().Build(hub);
        }
        private void Build(ArrivalHub250 source)
        {
            hub = source;
            wood = hub.Mat(new Color(.44f, .29f, .18f));
            darkWood = hub.Mat(new Color(.20f, .12f, .08f));
            snow = hub.Mat(new Color(.94f, .97f, 1f));
            metal = hub.Mat(new Color(.16f, .20f, .24f));
            glass = hub.Mat(new Color(.32f, .57f, .65f));
            blue = hub.Mat(new Color(.07f, .23f, .36f));
            white = hub.Mat(new Color(.94f, .94f, .86f));
            DecorateOffice();
            BuildShelter();
            // Sign supports stand outside the vehicle paths and pedestrian corridors.
            carCount = Sign("Parking_voitures", 106, 24, "P  VOITURES", "12 PLACES", 7f);
            busCount = Sign("Parking_autocars", 82, -51, "P  AUTOCARS", "3 PLACES", 8f);
            Sign("Accueil_station", 16, 28, "BIENVENUE", "FORFAITS  >", 5f);
            // Replace only the old floating labels; their kiosk and access stay in place.
            foreach (Transform child in hub.transform)
                if (child.name == "FORFAITS" || child.name == "P  VOITURES" || child.name == "P  AUTOCARS")
                    child.gameObject.SetActive(false);
        }
        private Transform Anchor(string name, float x, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(hub.Point(x, z), hub.Office.transform.rotation);
            return go.transform;
        }
        private GameObject Box(Transform parent, string name, Vector3 p, Vector3 size, Material material)
        { return hub.Box(name, parent, p, size, material); }
        private TextMesh Text(Transform parent, string name, string value, Vector3 position, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            // TextMesh's readable face is local -Z, matching the counter frontage.
            var text = go.AddComponent<TextMesh>();
            text.text = value; text.fontSize = 64; text.characterSize = size;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.color = Color.white;
            return text;
        }
        private void DecorateOffice()
        {
            Transform office = hub.Office.transform;
            Transform oldRoof = office.Find("Toit");
            if (oldRoof != null) oldRoof.gameObject.SetActive(false);
            var decor = new GameObject("Habillage_billetterie_256");
            decor.transform.SetParent(office, false);
            Transform t = decor.transform;
            // Horizontal facade boards, kept clear of the existing ticket window.
            for (int i = 0; i < 8; i++)
            {
                float y = .22f + i * .39f;
                Box(t, "Bardage_gauche", new Vector3(-2.32f, y, -2.03f), new Vector3(1.25f,.31f,.09f), wood);
                Box(t, "Bardage_droit", new Vector3(2.32f, y, -2.03f), new Vector3(1.25f,.31f,.09f), wood);
            }
            for (int i = -1; i <= 1; i += 2)
            {
                Box(t, "Montant_guichet", new Vector3(i * 1.56f,1.8f,-2.17f),new Vector3(.14f,1.4f,.2f),darkWood);
                Box(t, "Poteau_angle", new Vector3(i * 2.94f,1.6f,-2.08f),new Vector3(.18f,3.2f,.18f),darkWood);
            }
            Box(t,"Cadre_haut",new Vector3(0,2.45f,-2.17f),new Vector3(3.25f,.14f,.2f),darkWood);
            Box(t,"Comptoir",new Vector3(0,1.13f,-2.34f),new Vector3(3.4f,.18f,.7f),wood);
            Box(t,"Separation_guichets",new Vector3(0,1.8f,-2.17f),new Vector3(.12f,1.25f,.2f),darkWood);
            // A real gabled roof made of two inclined surfaces; snow caps follow the same pitch.
            for (int side = -1; side <= 1; side += 2)
            {
                var roof = Box(t,"Pan_toiture",new Vector3(side*1.76f,3.88f,0),new Vector3(3.76f,.24f,5.5f),darkWood);
                roof.transform.localRotation = Quaternion.Euler(0,0,-side*20f);
                var cap = Box(t,"Neige_toiture",new Vector3(side*1.76f,4.06f,0),new Vector3(3.8f,.13f,5.56f),snow);
                cap.transform.localRotation = roof.transform.localRotation;
            }
            Box(t,"Auvent",new Vector3(0,2.9f,-2.65f),new Vector3(4.2f,.15f,1.3f),metal);
            Box(t,"Enseigne",new Vector3(0,3.25f,-2.2f),new Vector3(4.7f,.62f,.16f),blue);
            Text(t,"Texte_forfaits","FORFAITS",new Vector3(0,3.25f,-2.30f),.28f);
            Text(t,"Texte_accueil","ACCUEIL",new Vector3(0,.65f,-2.10f),.20f);
        }
        private void BuildShelter()
        {
            // East end of the bus quay, away from all three passenger exit paths.
            Transform t = Anchor("Abri_autocars",98,-50);
            t.localRotation *= Quaternion.Euler(0,180,0);
            Box(t,"Socle",new Vector3(0,.06f,0),new Vector3(10,.12f,4),metal);
            Box(t,"Fond",new Vector3(0,1.7f,1.8f),new Vector3(9.6f,3.2f,.12f),glass);
            foreach (float x in new[]{-4.7f,4.7f})
            {
                Box(t,"Montant",new Vector3(x,1.7f,1.8f),new Vector3(.18f,3.4f,.18f),metal);
                Box(t,"Paroi_laterale",new Vector3(x,1.7f,.4f),new Vector3(.12f,3.1f,2.8f),glass);
            }
            Box(t,"Toit",new Vector3(0,3.48f,0),new Vector3(10.4f,.24f,4.4f),metal);
            Box(t,"Neige",new Vector3(0,3.65f,0),new Vector3(10.45f,.12f,4.45f),snow);
            Box(t,"Assise_banc",new Vector3(0,.8f,.8f),new Vector3(6,.2f,.7f),wood);
            Box(t,"Dossier_banc",new Vector3(0,1.25f,1.05f),new Vector3(6,.65f,.12f),wood);
            foreach(float x in new[]{-2.5f,2.5f}) Box(t,"Pied_banc",new Vector3(x,.4f,.8f),new Vector3(.15f,.8f,.5f),metal);
            Box(t,"Bandeau",new Vector3(0,3.04f,-2.08f),new Vector3(9.5f,.65f,.12f),blue);
            Text(t,"Texte_arret","ARRET AUTOCARS",new Vector3(0,3.04f,-2.17f),.32f);
        }
        private TextMesh Sign(string name,float x,float z,string title,string detail,float width)
        {
            Transform t=Anchor(name,x,z);
            Box(t,"Support",new Vector3(0,1.65f,0),new Vector3(.2f,3.3f,.2f),metal);
            Box(t,"Panneau",new Vector3(0,3.7f,0),new Vector3(width,2f,.2f),blue);
            Box(t,"Bord_superieur",new Vector3(0,4.72f,0),new Vector3(width+.1f,.08f,.25f),white);
            Text(t,"Titre",title,new Vector3(0,4.08f,-.12f),.28f);
            var front=Text(t,"Detail",detail,new Vector3(0,3.35f,-.12f),.22f);
            // Both road-facing and mountain-facing sides are readable.
            var back=new GameObject("Face_arriere");back.transform.SetParent(t,false);
            back.transform.localRotation=Quaternion.Euler(0,180,0);
            Text(back.transform,"Titre",title,new Vector3(0,4.08f,-.12f),.28f);
            Text(back.transform,"Detail",detail,new Vector3(0,3.35f,-.12f),.22f);
            return front;
        }
        private void Update()
        {
            if(hub==null){Destroy(gameObject);return;}
            if(Time.unscaledTime<refreshAt)return;
            refreshAt=Time.unscaledTime+1f;
            SetBothSides(carCount,hub.ParkedCars+" / 12 STATIONNES");
            SetBothSides(busCount,hub.ParkedBuses+" / 3 STATIONNES");
        }
        private static void SetBothSides(TextMesh front,string value)
        {
            if(front==null)return;
            front.text=value;
            var back=front.transform.parent.Find("Face_arriere/Detail");
            if(back!=null)back.GetComponent<TextMesh>().text=value;
        }
    }
}
