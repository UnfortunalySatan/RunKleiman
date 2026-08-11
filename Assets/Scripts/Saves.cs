using System.Collections.Generic;
using UnityEngine;
using YG;
namespace YG
{
    public partial class SavesYG
    {
        public int bestRunScore = 0;   // лучший результат за один забег (для лидерборда)
        public int playerScore = 0;    // общий баланс (для магазина)
        public List<int> purchasedMaterials = new List<int>(); // индексы купленных материалов
        public int selectedMaterialIndex = 0;
    }
}