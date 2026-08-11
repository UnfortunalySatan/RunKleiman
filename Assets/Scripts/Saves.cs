using System.Collections.Generic;
using UnityEngine;
using YG;

namespace YG
{
    public partial class SavesYG
    {
        public int bestRunScore = 0;   // лучший результат за один забег
        public int playerScore = 0;    // общий баланс
        public List<int> purchasedMaterials = new List<int>(); // индексы купленных материалов
        public int selectedMaterialIndex = 0;

        // Новые поля для WebGL-сохранения громкости (значения по умолчанию = 1)
        public float musicVolume = 1f;
        public float soundVolume = 1f;
    }
}
