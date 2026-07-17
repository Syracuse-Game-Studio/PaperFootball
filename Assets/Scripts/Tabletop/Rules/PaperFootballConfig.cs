using UnityEngine;

namespace PaperFootball.Tabletop.Rules
{
    [CreateAssetMenu(menuName = "Paper Football/Tabletop Rules", fileName = "PaperFootballRules")]
    public class PaperFootballConfig : ScriptableObject
    {
        [SerializeField] private PaperFootballRuleSet rules = new();

        public PaperFootballRuleSet Rules => rules;

        public PaperFootballRuleSet CreateRuntimeRules()
        {
            PaperFootballRuleSet runtimeRules = rules != null ? rules.Clone() : new PaperFootballRuleSet();
            runtimeRules.Sanitize();
            return runtimeRules;
        }

        private void OnValidate()
        {
            rules ??= new PaperFootballRuleSet();
            rules.Sanitize();
        }
    }
}
