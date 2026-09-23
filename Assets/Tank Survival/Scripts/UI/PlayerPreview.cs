using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace TankSurvival
{
    // Class handling on "slot" in the main menu, which is an entry that show a tank preview, display its stat and allow
    // to add that tank to the game or not and change who control it (p1, p2 or computer)
    public class PlayerPreview : MonoBehaviour
    {
        [Header("References")]
        public RectTransform m_TankPreviewPosition;     // The Transform on which to place the Tank preview so it display at the right place on screen
        public RectTransform m_ControlChoiceRoot;       // The root of which all the control choice buttons are parented to
        public GameObject TankPreview { get; private set; }  // The chassis preview instance (rotates as a whole)
        public GameObject ChassisPrefab { get; private set; }
        public GameObject TurretPrefab { get; private set; }
        private GameObject m_TurretInstance;            // Instance of the turret spawned in preview
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

        /// <summary>
        /// Set up the preview with a single combined tank prefab (legacy support).
        /// </summary>
        public void SetTankPreview(GameObject prefab)
        {
            SetTankPreview(prefab, null);
        }

        /// <summary>
        /// Set up the preview with separate chassis and turret prefabs.
        /// If turretPrefab is null, no turret is shown.
        /// </summary>
        public void SetTankPreview(GameObject chassisPrefab, GameObject turretPrefab)
        {
            // Destroy previous preview
            DestroyPreview();

            ChassisPrefab = chassisPrefab;
            TurretPrefab = turretPrefab;

            if (chassisPrefab == null)
                return;

            // Instantiate the chassis
            TankPreview = Instantiate(chassisPrefab);

            TurretMountPoint turretMount = TankPreview.GetComponentInChildren<TurretMountPoint>();

            // Move chassis to the right preview position
            if (m_TankPreviewPosition == null)
            {
                Debug.LogError("[PlayerPreview] m_TankPreviewPosition == null! Превью будет в (0,0,0)");
            }
            else
            {
                var position = m_MenuCamera.WorldToScreenPoint(m_TankPreviewPosition.position);
                TankPreview.transform.position =
                    m_MenuCamera.ScreenToWorldPoint(position) + Vector3.back * 3.0f;
            }

            // Find TurretMountPoint on the chassis and spawn turret there
            if (turretPrefab != null)
            {
                Transform turretPos = turretMount != null ? turretMount.transform : null;
                if (turretPos != null)
                {
                    m_TurretInstance = Instantiate(turretPrefab, turretPos);
                }
                else
                {
                    Debug.LogWarning("[PlayerPreview] На шасси не найден TurretMountPoint для крепления башни!");
                }
            }

            // Disable all audio sources
            var audioSources = TankPreview.GetComponentsInChildren<AudioSource>();
            foreach (var source in audioSources)
            {
                Destroy(source);
            }
        }

        private void DestroyPreview()
        {
            if (m_TurretInstance != null)
            {
                Destroy(m_TurretInstance);
                m_TurretInstance = null;
            }

            if (TankPreview != null)
            {
                Destroy(TankPreview);
                TankPreview = null;
            }
        }
    }
}