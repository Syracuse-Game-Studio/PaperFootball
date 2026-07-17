using System;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public class FootballRestDetector : MonoBehaviour
    {
        [SerializeField] private float linearVelocityThreshold = 0.08f;
        [SerializeField] private float angularVelocityThreshold = 0.25f;
        [SerializeField] private float requiredStillTime = 0.35f;

        private Rigidbody body;
        private float stillTimer;
        private bool emittedRest;

        public bool IsResting { get; private set; }

        public event Action RestDetected;

        public void Configure(PaperFootballRuleSet rules)
        {
            if (rules != null)
            {
                linearVelocityThreshold = rules.footballStoppingThreshold;
                angularVelocityThreshold = rules.angularStoppingThreshold;
                requiredStillTime = rules.requiredStillTime;
            }

            linearVelocityThreshold = Mathf.Max(0.001f, linearVelocityThreshold);
            angularVelocityThreshold = Mathf.Max(0.001f, angularVelocityThreshold);
            requiredStillTime = Mathf.Max(0.01f, requiredStillTime);
        }

        public void ResetDetector()
        {
            stillTimer = 0f;
            emittedRest = false;
            IsResting = false;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            bool belowThresholds = body.linearVelocity.magnitude <= linearVelocityThreshold &&
                                   body.angularVelocity.magnitude <= angularVelocityThreshold;

            if (belowThresholds)
            {
                stillTimer += Time.fixedDeltaTime;
            }
            else
            {
                stillTimer = 0f;
                emittedRest = false;
                IsResting = false;
            }

            if (!emittedRest && stillTimer >= requiredStillTime)
            {
                emittedRest = true;
                IsResting = true;
                RestDetected?.Invoke();
            }
        }
    }
}
