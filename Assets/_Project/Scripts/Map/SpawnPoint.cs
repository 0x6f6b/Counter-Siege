using UnityEngine;

namespace CounterSiege
{
    public class SpawnPoint : MonoBehaviour
    {
        public Team team;

        void OnDrawGizmos()
        {
            Gizmos.color = team == Team.Terrorist ? Color.yellow : Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        }
    }
}
