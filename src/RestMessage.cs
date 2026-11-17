using System;

namespace EyeGuard
{
    internal sealed class RestMessage
    {
        public readonly string Title;
        public readonly string Advice;

        private RestMessage(string title, string advice)
        {
            Title = title;
            Advice = advice;
        }

        private static readonly RestMessage[] Messages = new RestMessage[]
        {
            new RestMessage("眼睛申请带薪休假", "屏幕先自己待会儿，别急着回消息\n看看 6 米外，让视线出去兜个风"),
            new RestMessage("暂停营业，眼睛去摸鱼", "这次摸鱼有正当理由：休息时间到\n放下手机，看看远处的真实世界"),
            new RestMessage("先别卷，眼睛要下班", "任务可以排队，休息已经到号了\n望望远处，再轻轻眨几下眼"),
            new RestMessage("给眼睛放个迷你假", "不用订机票，也不用写请假条\n闭目歇一会儿，就当去度个小假"),
            new RestMessage("远方有请，屏幕退场", "窗外的风景还没收过你的门票\n找个 6 米外的地方，慢慢看一会儿"),
            new RestMessage("眨眨眼，别让眼睛加班", "眼睛不是摄像头，不用一直在线\n轻轻眨眼，再把视线交给远方"),
            new RestMessage("此刻宜发呆，忌盯屏", "今日份发呆额度，现已到账\n望望远处，顺便让肩膀松口气"),
            new RestMessage("眼睛电量低，请稍候", "充电方式：放下屏幕，看看远方\n手机也算屏幕，别偷偷换一块哦"),
            new RestMessage("眼睛已切换省电模式", "后台任务：闭目养神，放松肩膀\n这几分钟，世界可以自己运行"),
            new RestMessage("屏幕看累了？互相放过", "你去看风景，它留在原地反省\n把目光放远一点，轻松待一会儿"),
            new RestMessage("让视线出门散个步", "不用走很远，目光先出发就行\n看看 6 米外，别急着把焦点拉回来"),
            new RestMessage("这几分钟，发呆也算正事", "暂时不解决问题，也不解决待办\n闭目歇一会儿，给自己留点空白"),
            new RestMessage("眼睛：这班先上到这里", "辛苦盯屏这么久，该轮到你歇歇了\n看看远处，再轻轻活动一下肩膀"),
            new RestMessage("别急，灵感也在中场休息", "好点子可能正在赶来的路上\n先望望远处，不用一直刷新屏幕"),
            new RestMessage("暂时离线，快乐养眼", "消息先排队，眼睛先放假\n放下手机，闭目歇一小会儿"),
            new RestMessage("请查收一份发呆邀请", "地点：窗边、远方，或者闭上眼\n内容：什么都不做，也挺好")
        };

        private static readonly Random Random = new Random();
        private static int _lastIndex = -1;

        public static RestMessage Next()
        {
            // Pick once per rest, excluding the previous message.
            int index = Random.Next(_lastIndex < 0 ? Messages.Length : Messages.Length - 1);
            if (_lastIndex >= 0 && index >= _lastIndex) index++;
            _lastIndex = index;
            return Messages[index];
        }
    }
}
