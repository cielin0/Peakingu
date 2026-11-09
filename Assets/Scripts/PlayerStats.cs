using System;
using UnityEngine;

namespace Peakingu
{
    public enum UpgradeType
    {
        GrapplingHook,   // Mano con Gancho (30 monedas)
        JetBoots,        // Propulsores de Salto (20 monedas)
        AntiSlipGloves,  // Guantes Antideslizantes (15 monedas)
        Hydrodash        // Módulo de Dash Mejorado (25 monedas)
    }

    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("Mejoras de Habilidad (Sin Inventario - Pasivas Permanentes)")]
        [Tooltip("Mano con Gancho Cyborg (Costo: 30). Permite anclarse y escalar con gancho.")]
        public bool hasGrapplingHook = false;

        [Tooltip("Propulsores de Salto / Jet-Boots (Costo: 20). Incrementa salto y otorga doble salto.")]
        public bool hasJetBoots = false;

        [Tooltip("Guantes Antideslizantes (Costo: 15). Reduce la inercia y deslizamiento en hielo.")]
        public bool hasAntiSlipGloves = false;

        [Tooltip("Módulo de Dash Mejorado / Hidrodash (Costo: 25). Reduce el cooldown y alarga el dash.")]
        public bool hasHydrodash = false;

        // Eventos públicos
        public static event Action<UpgradeType> OnUpgradeUnlocked;
        public static event Action OnStatsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool HasUpgrade(UpgradeType upgrade)
        {
            return upgrade switch
            {
                UpgradeType.GrapplingHook => hasGrapplingHook,
                UpgradeType.JetBoots => hasJetBoots,
                UpgradeType.AntiSlipGloves => hasAntiSlipGloves,
                UpgradeType.Hydrodash => hasHydrodash,
                _ => false
            };
        }

        public void UnlockUpgrade(UpgradeType upgrade)
        {
            switch (upgrade)
            {
                case UpgradeType.GrapplingHook:
                    hasGrapplingHook = true;
                    break;
                case UpgradeType.JetBoots:
                    hasJetBoots = true;
                    break;
                case UpgradeType.AntiSlipGloves:
                    hasAntiSlipGloves = true;
                    break;
                case UpgradeType.Hydrodash:
                    hasHydrodash = true;
                    break;
            }

            // Aplicar de inmediato las modificaciones al PlayerController activo
            ApplyUpgradesToPlayer();

            OnUpgradeUnlocked?.Invoke(upgrade);
            OnStatsChanged?.Invoke();
            Debug.Log($"[PlayerStats] ¡Mejora desbloqueada permanentemente: {upgrade}!");
        }

        public void ApplyUpgradesToPlayer()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) return;

            var controller = playerObj.GetComponent<PlayerController>();
            if (controller == null) return;

            // 1. Mejora Hidrodash: Dash con cooldown acortado y más impulso
            if (hasHydrodash)
            {
                controller.dashCooldown = 0.5f; // Reduce enfriamiento de 1.0s a 0.5s
                controller.dashSpeed = 22f;     // Aumenta velocidad de 18f a 22f
            }

            // 2. Mejora Jet-Boots: Mayor altura de salto base y doble salto
            if (hasJetBoots)
            {
                controller.EnableJetBoots(true);
            }

            // 3. Mejora Guantes Antideslizantes: Máxima tracción y frenado
            if (hasAntiSlipGloves)
            {
                controller.EnableAntiSlipGloves(true);
            }
        }

        public void ResetStats()
        {
            hasGrapplingHook = false;
            hasJetBoots = false;
            hasAntiSlipGloves = false;
            hasHydrodash = false;
            OnStatsChanged?.Invoke();
        }
    }
}
