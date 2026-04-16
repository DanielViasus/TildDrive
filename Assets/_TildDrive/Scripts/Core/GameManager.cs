using UnityEngine;
using UnityEngine.SceneManagement;

namespace TiltDrive.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Escena inicial de pruebas")]
        [SerializeField] private string nombreEscenaInicial = "VehicleTestScene";

        private void Awake()
        {
            Debug.Log("[TiltDrive] GameManager inicializado correctamente.");
        }

        private void Start()
        {
            CargarEscenaInicial();
        }

        private void CargarEscenaInicial()
        {
            Scene escenaActual = SceneManager.GetActiveScene();

            if (escenaActual.name == nombreEscenaInicial)
            {
                Debug.Log("[TiltDrive] Ya se encuentra en la escena inicial.");
                return;
            }

            Debug.Log($"[TiltDrive] Cargando escena inicial: {nombreEscenaInicial}");
            SceneManager.LoadScene(nombreEscenaInicial);
        }
    }
}