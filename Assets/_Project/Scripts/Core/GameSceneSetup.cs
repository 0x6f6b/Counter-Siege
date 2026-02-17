using UnityEngine;
using UnityEngine.InputSystem;

namespace CounterSiege
{
    public class GameSceneSetup : MonoBehaviour
    {
        [Tooltip("Assign the InputSystem_Actions asset here")]
        public InputActionAsset inputActions;

        void Awake()
        {
            // Store reference for bootstrapper
            _inputActions = inputActions;

            // Launch bootstrapper
            gameObject.AddComponent<GameBootstrapper>();
        }

        // Static access for bootstrapper
        internal static InputActionAsset _inputActions;
    }
}
