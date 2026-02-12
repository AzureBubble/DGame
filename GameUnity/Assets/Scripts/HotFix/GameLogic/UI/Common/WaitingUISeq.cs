namespace GameLogic
{
    public class WaitingUISeq
    {
        public const uint DIR_WAIT_SEQID = 1000000;
        public const uint AUTH_WAIT_SEQID = 1000001;
        public const uint LOGINWORLD_SEQID = 1000002;       //连接到游戏服务器的等待
        public const uint ASYNC_LOAD_UI = 1000003;          //异步载入界面
        public const uint PLATFORM_LOGIN = 1000004;         //平台登录
        public const uint ASYNC_LOAD_CONFIG = 1000005;      //读取通用的配置文件
        public const uint GRAY_UPDATE_WAIT = 1000006;       // 灰度更新等待
        public const uint RELAY_BATTLE = 1000007;           //联网战斗的一些状态显示
        public const uint SDK_UPLOAD_IMAGE = 1000008;       // 上传图片
        public const uint WAIT_SHOW_AD_SEQID = 1000009;     //等待广告
    }
}