using SuperScrollView;

namespace GameLogic
{
    public class UILoopItemWidget : SelectItemBase
    {
        public LoopListViewItem2 LoopItem { set; get; }

        public int Index { private set; get; }

        public virtual void UpdateItem(int index)
        {
            Index = index;
        }
    }
}