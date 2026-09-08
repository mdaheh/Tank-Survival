using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace Tanks.Complete
{
    // Class handling on "slot" in the main menu, which is an entry that show a tank preview, display its stat and allow
    // to add that tank to the game or not and change who control it (p1, p2 or computer)
    public class PlayerPreview : MonoBehaviour
    {
        [Header("References")]
        public RectTransform m_TankPreviewPosition;     // The Transform on which to place the Tank preview so it display at the right place on screen
        public RectTransform m_ControlChoiceRoot;       // The root of which all the control choice buttons are parented to
        public GameObject TankPreview { get; set; }         // The preview instance that show this tank rotating in the menu
        public GameObject TankPrefab { get; private set; }  // The prefab this slot is based on
        private Camera m_MenuCamera;                        // The Camera used to display the menu

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            m_MenuCamera = GetComponentInParent<Camera>();
        }
        
        private void Update()
        {
            // If we have a preview slowly rotate it
            if (TankPreview != null)
            {
                TankPreview.transform.Rotate(Vector3.up, 45.0f * Time.deltaTime);
            }
        }

        public void SetTankPreview(GameObject prefab)
        {
            // If we already have a tank preview, destroy it
            if (TankPreview != null)
            {
                Destroy(TankPreview);
            }

            //assign the right prefab
            TankPrefab = prefab;
            //then instantiate it as the preview
            TankPreview = Instantiate(prefab);
            
            //move it to the right preview position so it appears in the right spot on screen
            var position = m_MenuCamera.WorldToScreenPoint(m_TankPreviewPosition.position);
            TankPreview.transform.position =
                m_MenuCamera.ScreenToWorldPoint(position) + Vector3.back * 3.0f;
            
            // go through all renderers of that tank
            // MeshRenderer[] renderers = TankPreview.GetComponentsInChildren<MeshRenderer>();
            // for (int i = 0; i < renderers.Length; i++)
            // {
            //     var renderer = renderers[i];
            //     for (int j = 0; j < renderer.materials.Length; ++j)
            //     {
            //         // then when we find the TankColor material
            //         if (renderer.materials[j].name.Contains("TankColor"))
            //         {
            //             // Set its color to the slot color
            //             renderer.materials[j].color = m_SlotColor;
            //         }
            //     }
            // }
            
            //Disable all audio
            var audioSource = TankPreview.GetComponentsInChildren<AudioSource>();
            foreach (var source in audioSource)
            {
                Destroy(source);
            }
        }
    }
}