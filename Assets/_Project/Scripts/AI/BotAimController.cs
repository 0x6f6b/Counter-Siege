using UnityEngine;

namespace CounterSiege
{
    public class BotAimController : MonoBehaviour
    {
        public BotDifficulty difficulty = BotDifficulty.Normal;
        public float aimSpeed = 6f;
        public float aimInaccuracy = 0.5f;

        Vector3 aimOffset;
        float offsetChangeTimer;

        void Start()
        {
            var settings = GameManager.Instance?.settings;
            if (settings != null)
                difficulty = settings.defaultDifficulty;

            ApplyDifficulty();
        }

        void ApplyDifficulty()
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    aimSpeed = 3f;
                    aimInaccuracy = 0.9f;
                    break;
                case BotDifficulty.Normal:
                    aimSpeed = 6f;
                    aimInaccuracy = 0.5f;
                    break;
                case BotDifficulty.Hard:
                    aimSpeed = 12f;
                    aimInaccuracy = 0.2f;
                    break;
            }
        }

        public void AimAt(Vector3 targetPosition)
        {
            // Update random offset periodically
            offsetChangeTimer -= Time.deltaTime;
            if (offsetChangeTimer <= 0)
            {
                offsetChangeTimer = 0.5f;
                aimOffset = Random.insideUnitSphere * aimInaccuracy;
            }

            Vector3 aimTarget = targetPosition + aimOffset;
            Vector3 direction = (aimTarget - transform.position).normalized;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, aimSpeed * Time.deltaTime * 60f);
            }
        }

        public bool IsAimedAt(Vector3 target, float threshold = 10f)
        {
            Vector3 toTarget = (target - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toTarget);
            return angle < threshold;
        }
    }
}
