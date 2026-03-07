namespace GameBattle
{
    /// <summary>
    /// 渲染层对象接口
    /// </summary>
    public interface IRenderUnit
    {
        /// <summary>
        /// 派发事件通知
        /// </summary>
        /// <param name="eventId">事件ID</param>
        void OnUnitEvent(int eventId);
    }
}
