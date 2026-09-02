using UnityEngine;

namespace PokeIdle
{
    [CreateAssetMenu(fileName = "Move", menuName = "PokeIdle/Move Definition")]
    public sealed class MoveDefinition : ScriptableObject
    {
        [Header("Identity")]
        public int Id;
        public string MoveName;
        public ElementalType Type;

        [Header("Combat")]
        public MoveCategory Category = MoveCategory.Physical;
        public int Power = 40;
        [Range(1, 100)] public int Accuracy = 100;
        public int MaxPP = 20;
        public int Priority;

        public bool DealsDamage
        {
            get { return Category != MoveCategory.Status && Power > 0; }
        }
    }
}
