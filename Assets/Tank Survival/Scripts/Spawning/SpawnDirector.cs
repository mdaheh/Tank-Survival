using System.Collections.Generic;
using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Директор точек/зон спавна врагов (T032).
    /// N мест расставляет человек в сцене/Inspector; число масштабируется списком, без правки кода.
    /// Заменяет прежний хардкод «круг радиусом 15 м вокруг игрока» — позиции стали данными.
    /// Выбор места — Random без аллокаций и без Physics-запросов.
    /// </summary>
    public class SpawnDirector : MonoBehaviour
    {
        /// <summary>
        /// Место спавна: точка (radius = 0) или зона (диск радиусом radius в плоскости XZ).
        /// </summary>
        [System.Serializable]
        public class SpawnArea
        {
            public Transform point;         // Позиция спавна — расставляет человек
            [Min(0f)] public float radius;  // 0 — точка; >0 — зона вокруг точки
        }

        [Tooltip("Точки/зоны спавна врагов (дефолт в сцене — 5 штук). Число меняется списком, код не правится.")]
        [SerializeField] private List<SpawnArea> m_SpawnAreas = new List<SpawnArea>();

        /// <summary>
        /// Число мест в списке (в т.ч. незаполненных). Для диагностики/лога-гарда вызывающего.
        /// </summary>
        public int AreaCount => m_SpawnAreas.Count;

        /// <summary>
        /// Случайная позиция спавна из точек/зон. Без аллокаций и Physics-запросов.
        /// Возврат false — если ни одно место не задано (вызывающий решает, что делать).
        /// </summary>
        public bool TryGetSpawnPosition(out Vector3 position)
        {
            int count = m_SpawnAreas.Count;

            // Случайный старт + линейный проход: даёт равномерный выбор без аллокаций
            // и гарантированно находит валидное место, если оно есть.
            int start = Random.Range(0, count);
            for (int i = 0; i < count; i++)
            {
                SpawnArea area = m_SpawnAreas[(start + i) % count];
                if (area == null || area.point == null) continue;

                position = area.point.position;
                if (area.radius > 0f)
                {
                    // Зона: равномерная точка в диске вокруг заданной точки (плоскость XZ)
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float distance = Mathf.Sqrt(Random.Range(0f, 1f)) * area.radius;
                    position.x += Mathf.Cos(angle) * distance;
                    position.z += Mathf.Sin(angle) * distance;
                }

                return true;
            }

            position = Vector3.zero;
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Подсказка при расстановке точек человеком (T032): где появятся враги.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < m_SpawnAreas.Count; i++)
            {
                SpawnArea area = m_SpawnAreas[i];
                if (area == null || area.point == null) continue;

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(area.point.position, Mathf.Max(0.5f, area.radius));
            }
        }
#endif
    }
}
