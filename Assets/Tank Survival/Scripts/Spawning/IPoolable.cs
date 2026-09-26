namespace TankSurvival
{
    /// <summary>
    /// Контракт объекта, работающего в пуле (T024a).
    /// Состояние сбрасывает сам объект; вызывает PoolManager
    /// в actionOnGet / actionOnRelease.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Сброс состояния при взятии из пула — единственная точка сброса.</summary>
        void OnGetFromPool();

        /// <summary>Сброс состояния при возврате в пул.</summary>
        void OnReleaseFromPool();
    }
}
