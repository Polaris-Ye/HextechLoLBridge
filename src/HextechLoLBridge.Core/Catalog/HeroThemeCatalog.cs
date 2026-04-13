using System.Globalization;
using HextechLoLBridge.Core.Models;

namespace HextechLoLBridge.Core.Catalog;

public static class HeroThemeCatalog
{
    private const string ChampionIconBase = "https://ddragon.leagueoflegends.com/cdn/16.7.1/img/champion/";
    private sealed record ChampionTheme(string ChampionKey, string DisplayName, string DefaultHex, string ThemeLabel, string? IconChampionId, params string[] Aliases);

    private static readonly ChampionTheme[] ChampionThemes = new[]
    {
        new ChampionTheme("Lissandra", "丽桑卓", "#3AA8FF", "默认主题", "Lissandra", "Lissandra", "丽桑卓", "冰霜女巫"),
        new ChampionTheme("Udyr", "乌迪尔", "#3AA8FF", "默认主题", "Udyr", "Udyr", "乌迪尔", "兽灵行者"),
        new ChampionTheme("Leblanc", "乐芙兰", "#3AA8FF", "默认主题", "Leblanc", "LeBlanc", "Leblanc", "乐芙兰", "诡术妖姬"),
        new ChampionTheme("Zaahen", "亚恒", "#D7B55B", "圣冥金", "Zaahen", "Zaahen", "不灭冥圣", "亚恒"),
        new ChampionTheme("Aatrox", "亚托克斯", "#3AA8FF", "默认主题", "Aatrox", "Aatrox", "亚托克斯", "暗裔剑魔"),
        new ChampionTheme("Yasuo", "亚索", "#53B8FF", "疾风蓝", "Yasuo", "Yasuo", "亚索", "疾风剑豪"),
        new ChampionTheme("Ezreal", "伊泽瑞尔", "#3AA8FF", "默认主题", "Ezreal", "Ezreal", "伊泽瑞尔", "探险家"),
        new ChampionTheme("Evelynn", "伊芙琳", "#3AA8FF", "默认主题", "Evelynn", "Evelynn", "伊芙琳", "痛苦之拥"),
        new ChampionTheme("Elise", "伊莉丝", "#3AA8FF", "默认主题", "Elise", "Elise", "伊莉丝", "蜘蛛女皇"),
        new ChampionTheme("Zoe", "佐伊", "#3AA8FF", "默认主题", "Zoe", "Zoe", "佐伊", "暮光星灵"),
        new ChampionTheme("Viego", "佛耶戈", "#55D2B3", "破败青绿", "Viego", "Viego", "佛耶戈", "破败之王"),
        new ChampionTheme("Illaoi", "俄洛伊", "#3AA8FF", "默认主题", "Illaoi", "Illaoi", "俄洛伊", "海兽祭司"),
        new ChampionTheme("KogMaw", "克格莫", "#3AA8FF", "默认主题", "KogMaw", "Kog'Maw", "KogMaw", "克格莫", "深渊巨口"),
        new ChampionTheme("Kled", "克烈", "#3AA8FF", "默认主题", "Kled", "Kled", "克烈", "暴怒骑士"),
        new ChampionTheme("Rumble", "兰博", "#3AA8FF", "默认主题", "Rumble", "Rumble", "兰博", "机械公敌"),
        new ChampionTheme("Nasus", "内瑟斯", "#3AA8FF", "默认主题", "Nasus", "Nasus", "内瑟斯", "沙漠死神"),
        new ChampionTheme("Kennen", "凯南", "#3AA8FF", "默认主题", "Kennen", "Kennen", "凯南", "狂暴之心"),
        new ChampionTheme("Kayle", "凯尔", "#3AA8FF", "默认主题", "Kayle", "Kayle", "凯尔", "审判天使"),
        new ChampionTheme("Caitlyn", "凯特琳", "#3AA8FF", "默认主题", "Caitlyn", "Caitlyn", "凯特琳", "皮城女警"),
        new ChampionTheme("Kayn", "凯隐", "#3AA8FF", "默认主题", "Kayn", "Kayn", "凯隐", "影流之镰"),
        new ChampionTheme("Galio", "加里奥", "#3AA8FF", "默认主题", "Galio", "Galio", "加里奥", "正义巨像"),
        new ChampionTheme("Nunu", "努努和威朗普", "#3AA8FF", "默认主题", "Nunu", "Nunu", "Nunu & Willump", "努努", "努努和威朗普", "雪原双子"),
        new ChampionTheme("Zed", "劫", "#3AA8FF", "默认主题", "Zed", "Zed", "劫", "影流之主"),
        new ChampionTheme("Kindred", "千珏", "#3AA8FF", "默认主题", "Kindred", "Kindred", "千珏", "永猎双子"),
        new ChampionTheme("Belveth", "卑尔维斯", "#3AA8FF", "默认主题", "Belveth", "Bel'Veth", "BelVeth", "Belveth", "卑尔维斯", "虚空女皇"),
        new ChampionTheme("Khazix", "卡兹克", "#3AA8FF", "默认主题", "Khazix", "Kha'Zix", "Khazix", "卡兹克", "虚空掠夺者"),
        new ChampionTheme("Karma", "卡尔玛", "#3AA8FF", "默认主题", "Karma", "Karma", "卡尔玛", "天启者"),
        new ChampionTheme("Karthus", "卡尔萨斯", "#3AA8FF", "默认主题", "Karthus", "Karthus", "卡尔萨斯", "死亡颂唱者"),
        new ChampionTheme("Katarina", "卡特琳娜", "#3AA8FF", "默认主题", "Katarina", "Katarina", "不祥之刃", "卡特琳娜"),
        new ChampionTheme("Kalista", "卡莉斯塔", "#3AA8FF", "默认主题", "Kalista", "Kalista", "卡莉斯塔", "复仇之矛"),
        new ChampionTheme("KaiSa", "卡莎", "#3AA8FF", "默认主题", "Kaisa", "Kai'Sa", "KaiSa", "Kaisa", "卡莎", "虚空之女"),
        new ChampionTheme("Kassadin", "卡萨丁", "#3AA8FF", "默认主题", "Kassadin", "Kassadin", "卡萨丁", "虚空行者"),
        new ChampionTheme("Camille", "卡蜜尔", "#3AA8FF", "默认主题", "Camille", "Camille", "卡蜜尔", "青钢影"),
        new ChampionTheme("Cassiopeia", "卡西奥佩娅", "#3AA8FF", "默认主题", "Cassiopeia", "Cassiopeia", "卡西奥佩娅", "魔蛇之拥"),
        new ChampionTheme("Lucian", "卢锡安", "#3AA8FF", "默认主题", "Lucian", "Lucian", "卢锡安", "圣枪游侠"),
        new ChampionTheme("Urgot", "厄加特", "#3AA8FF", "默认主题", "Urgot", "Urgot", "厄加特", "无畏战车"),
        new ChampionTheme("Aphelios", "厄斐琉斯", "#3AA8FF", "默认主题", "Aphelios", "Aphelios", "厄斐琉斯", "残月之肃"),
        new ChampionTheme("MissFortune", "厄运小姐", "#3AA8FF", "默认主题", "MissFortune", "Miss Fortune", "MissFortune", "厄运小姐", "赏金猎人"),
        new ChampionTheme("Gragas", "古拉加斯", "#3AA8FF", "默认主题", "Gragas", "Gragas", "古拉加斯", "酒桶"),
        new ChampionTheme("Ziggs", "吉格斯", "#3AA8FF", "默认主题", "Ziggs", "Ziggs", "吉格斯", "爆破鬼才"),
        new ChampionTheme("JarvanIV", "嘉文四世", "#3AA8FF", "默认主题", "JarvanIV", "Jarvan IV", "JarvanIV", "嘉文四世", "德玛西亚皇子"),
        new ChampionTheme("Twitch", "图奇", "#3AA8FF", "默认主题", "Twitch", "Twitch", "图奇", "瘟疫之源"),
        new ChampionTheme("Zilean", "基兰", "#3AA8FF", "默认主题", "Zilean", "Zilean", "基兰", "时光守护者"),
        new ChampionTheme("TahmKench", "塔姆·肯奇", "#3AA8FF", "默认主题", "TahmKench", "Tahm Kench", "TahmKench", "塔姆·肯奇", "河流之主"),
        new ChampionTheme("Taliyah", "塔莉垭", "#3AA8FF", "默认主题", "Taliyah", "Taliyah", "塔莉垭", "岩雀"),
        new ChampionTheme("Taric", "塔里克", "#3AA8FF", "默认主题", "Taric", "Taric", "塔里克", "宝石骑士"),
        new ChampionTheme("Sylas", "塞拉斯", "#3AA8FF", "默认主题", "Sylas", "Sylas", "塞拉斯", "解脱者"),
        new ChampionTheme("Malphite", "墨菲特", "#3AA8FF", "默认主题", "Malphite", "Malphite", "墨菲特", "熔岩巨兽"),
        new ChampionTheme("Qiyana", "奇亚娜", "#3AA8FF", "默认主题", "Qiyana", "Qiyana", "不羁之悦", "奇亚娜"),
        new ChampionTheme("Nidalee", "奈德丽", "#3AA8FF", "默认主题", "Nidalee", "Nidalee", "奈德丽", "狂野女猎手"),
        new ChampionTheme("Quinn", "奎因", "#3AA8FF", "默认主题", "Quinn", "Quinn", "奎因", "德玛西亚之翼"),
        new ChampionTheme("KSante", "奎桑提", "#3AA8FF", "默认主题", "KSante", "K'Sante", "KSante", "奎桑提", "纳祖芒荣耀"),
        new ChampionTheme("Ornn", "奥恩", "#3AA8FF", "默认主题", "Ornn", "Ornn", "奥恩", "山隐之焰"),
        new ChampionTheme("Olaf", "奥拉夫", "#3AA8FF", "默认主题", "Olaf", "Olaf", "奥拉夫", "狂战士"),
        new ChampionTheme("AurelionSol", "奥瑞利安·索尔", "#3AA8FF", "默认主题", "AurelionSol", "Aurelion Sol", "AurelionSol", "奥瑞利安·索尔", "铸星龙王"),
        new ChampionTheme("Orianna", "奥莉安娜", "#3AA8FF", "默认主题", "Orianna", "Orianna", "发条魔灵", "奥莉安娜"),
        new ChampionTheme("Neeko", "妮蔻", "#3AA8FF", "默认主题", "Neeko", "Neeko", "万花通灵", "妮蔻"),
        new ChampionTheme("Sona", "娑娜", "#67B4FF", "琴瑟蓝", "Sona", "Sona", "娑娜", "琴瑟仙女"),
        new ChampionTheme("Nami", "娜美", "#3AA8FF", "默认主题", "Nami", "Nami", "唤潮鲛姬", "娜美"),
        new ChampionTheme("Zyra", "婕拉", "#3AA8FF", "默认主题", "Zyra", "Zyra", "婕拉", "荆棘之兴"),
        new ChampionTheme("MonkeyKing", "孙悟空", "#3AA8FF", "默认主题", "MonkeyKing", "MonkeyKing", "Wukong", "孙悟空", "齐天大圣"),
        new ChampionTheme("Annie", "安妮", "#3AA8FF", "默认主题", "Annie", "Annie", "安妮", "黑暗之女"),
        new ChampionTheme("Ambessa", "安蓓萨", "#B85A4A", "狼母棕红", "Ambessa", "Ambessa", "安比萨", "安蓓萨", "铁血狼母"),
        new ChampionTheme("Nilah", "尼菈", "#3AA8FF", "默认主题", "Nilah", "Nilah", "尼菈", "涤魂圣枪"),
        new ChampionTheme("Tristana", "崔丝塔娜", "#3AA8FF", "默认主题", "Tristana", "Tristana", "崔丝塔娜", "麦林炮手"),
        new ChampionTheme("TwistedFate", "崔斯特", "#3AA8FF", "默认主题", "TwistedFate", "Twisted Fate", "TwistedFate", "卡牌大师", "崔斯特"),
        new ChampionTheme("Bard", "巴德", "#3AA8FF", "默认主题", "Bard", "Bard", "巴德", "星界游神"),
        new ChampionTheme("Brand", "布兰德", "#3AA8FF", "默认主题", "Brand", "Brand", "复仇焰魂", "布兰德"),
        new ChampionTheme("Blitzcrank", "布里茨", "#3AA8FF", "默认主题", "Blitzcrank", "Blitzcrank", "布里茨", "蒸汽机器人"),
        new ChampionTheme("Braum", "布隆", "#3AA8FF", "默认主题", "Braum", "Braum", "布隆", "弗雷尔卓德之心"),
        new ChampionTheme("Shyvana", "希瓦娜", "#3AA8FF", "默认主题", "Shyvana", "Shyvana", "希瓦娜", "龙血武姬"),
        new ChampionTheme("Sivir", "希维尔", "#3AA8FF", "默认主题", "Sivir", "Sivir", "希维尔", "战争女神"),
        new ChampionTheme("Corki", "库奇", "#3AA8FF", "默认主题", "Corki", "Corki", "库奇", "英勇投弹手"),
        new ChampionTheme("Vladimir", "弗拉基米尔", "#3AA8FF", "默认主题", "Vladimir", "Vladimir", "弗拉基米尔", "猩红收割者"),
        new ChampionTheme("Hwei", "彗", "#3AA8FF", "默认主题", "Hwei", "Hwei", "彗", "绘画大师"),
        new ChampionTheme("Darius", "德莱厄斯", "#3AA8FF", "默认主题", "Darius", "Darius", "德莱厄斯", "诺克萨斯之手"),
        new ChampionTheme("Draven", "德莱文", "#3AA8FF", "默认主题", "Draven", "Draven", "德莱文", "荣耀行刑官"),
        new ChampionTheme("Yuumi", "悠米", "#3AA8FF", "默认主题", "Yuumi", "Yuumi", "悠米", "魔法猫咪"),
        new ChampionTheme("Shen", "慎", "#3AA8FF", "默认主题", "Shen", "Shen", "慎", "暮光之眼"),
        new ChampionTheme("Diana", "戴安娜", "#3AA8FF", "默认主题", "Diana", "Diana", "戴安娜", "皎月女神"),
        new ChampionTheme("Zac", "扎克", "#3AA8FF", "默认主题", "Zac", "Zac", "扎克", "生化魔人"),
        new ChampionTheme("Lux", "拉克丝", "#F4D96B", "光辉金", "Lux", "Lux", "光辉女郎", "拉克丝"),
        new ChampionTheme("Rammus", "拉莫斯", "#3AA8FF", "默认主题", "Rammus", "Rammus", "披甲龙龟", "拉莫斯"),
        new ChampionTheme("Teemo", "提莫", "#3AA8FF", "默认主题", "Teemo", "Teemo", "提莫", "迅捷斥候"),
        new ChampionTheme("Skarner", "斯卡纳", "#3AA8FF", "默认主题", "Skarner", "Skarner", "斯卡纳", "水晶先锋"),
        new ChampionTheme("Swain", "斯维因", "#3AA8FF", "默认主题", "Swain", "Swain", "斯维因", "诺克萨斯统领"),
        new ChampionTheme("Smolder", "斯莫德", "#3AA8FF", "默认主题", "Smolder", "Smolder", "斯莫德", "炽焰雏龙"),
        new ChampionTheme("MasterYi", "易", "#3AA8FF", "默认主题", "MasterYi", "Master Yi", "MasterYi", "无极剑圣", "易"),
        new ChampionTheme("Gangplank", "普朗克", "#3AA8FF", "默认主题", "Gangplank", "Gangplank", "普朗克", "海洋之灾"),
        new ChampionTheme("LeeSin", "李青", "#3AA8FF", "默认主题", "LeeSin", "Lee Sin", "LeeSin", "李青", "盲僧"),
        new ChampionTheme("Jayce", "杰斯", "#3AA8FF", "默认主题", "Jayce", "Jayce", "未来守护者", "杰斯"),
        new ChampionTheme("Gwen", "格温", "#3AA8FF", "默认主题", "Gwen", "Gwen", "格温", "灵罗娃娃"),
        new ChampionTheme("Graves", "格雷福斯", "#3AA8FF", "默认主题", "Graves", "Graves", "格雷福斯", "法外狂徒"),
        new ChampionTheme("Mel", "梅尔", "#F0C86B", "诺城金", "Mel", "Mel", "光耀诺克萨斯", "梅尔", "梅尔·米达尔达"),
        new ChampionTheme("Yone", "永恩", "#C24B5A", "猩红酒红", "Yone", "Yone", "封魔剑魂", "永恩"),
        new ChampionTheme("Volibear", "沃利贝尔", "#3AA8FF", "默认主题", "Volibear", "Volibear", "沃利贝尔", "雷霆咆哮"),
        new ChampionTheme("Warwick", "沃里克", "#3AA8FF", "默认主题", "Warwick", "Warwick", "沃里克", "祖安怒兽"),
        new ChampionTheme("Poppy", "波比", "#3AA8FF", "默认主题", "Poppy", "Poppy", "圣锤之毅", "波比"),
        new ChampionTheme("Tryndamere", "泰达米尔", "#3AA8FF", "默认主题", "Tryndamere", "Tryndamere", "泰达米尔", "蛮族之王"),
        new ChampionTheme("Talon", "泰隆", "#3AA8FF", "默认主题", "Talon", "Talon", "刀锋之影", "泰隆"),
        new ChampionTheme("Zeri", "泽丽", "#58F0A7", "灵光翠青", "Zeri", "Zeri", "泽丽", "祖安花火"),
        new ChampionTheme("Xerath", "泽拉斯", "#3AA8FF", "默认主题", "Xerath", "Xerath", "泽拉斯", "远古巫灵"),
        new ChampionTheme("Rakan", "洛", "#3AA8FF", "默认主题", "Rakan", "Rakan", "幻翎", "洛"),
        new ChampionTheme("Pyke", "派克", "#3AA8FF", "默认主题", "Pyke", "Pyke", "派克", "血港鬼影"),
        new ChampionTheme("Pantheon", "潘森", "#3AA8FF", "默认主题", "Pantheon", "Pantheon", "战争之王", "潘森"),
        new ChampionTheme("Renata", "烈娜塔·戈拉斯克", "#3AA8FF", "默认主题", "Renata", "Renata", "Renata Glasc", "炼金男爵", "烈娜塔", "烈娜塔·戈拉斯克"),
        new ChampionTheme("Jhin", "烬", "#3AA8FF", "默认主题", "Jhin", "Jhin", "戏命师", "烬"),
        new ChampionTheme("Trundle", "特朗德尔", "#3AA8FF", "默认主题", "Trundle", "Trundle", "巨魔之王", "特朗德尔"),
        new ChampionTheme("Malzahar", "玛尔扎哈", "#3AA8FF", "默认主题", "Malzahar", "Malzahar", "玛尔扎哈", "虚空先知"),
        new ChampionTheme("Ryze", "瑞兹", "#3AA8FF", "默认主题", "Ryze", "Ryze", "瑞兹", "符文法师"),
        new ChampionTheme("Sejuani", "瑟庄妮", "#3AA8FF", "默认主题", "Sejuani", "Sejuani", "北地之怒", "瑟庄妮"),
        new ChampionTheme("Sett", "瑟提", "#D28C54", "斗兽铜", "Sett", "Sett", "瑟提", "腕豪"),
        new ChampionTheme("Lulu", "璐璐", "#3AA8FF", "默认主题", "Lulu", "Lulu", "仙灵女巫", "璐璐"),
        new ChampionTheme("Garen", "盖伦", "#5AA7FF", "德邦蓝", "Garen", "Garen", "德玛西亚之力", "盖伦"),
        new ChampionTheme("ChoGath", "科'加斯", "#3AA8FF", "默认主题", "Chogath", "Cho'Gath", "ChoGath", "科'加斯", "科加斯", "虚空恐惧"),
        new ChampionTheme("Milio", "米利欧", "#5EEA97", "暖青绿", "Milio", "Milio", "明烛", "米利欧"),
        new ChampionTheme("Soraka", "索拉卡", "#3AA8FF", "默认主题", "Soraka", "Soraka", "众星之子", "索拉卡"),
        new ChampionTheme("Yorick", "约里克", "#3AA8FF", "默认主题", "Yorick", "Yorick", "掘墓者", "约里克"),
        new ChampionTheme("Yunara", "芸阿娜", "#58F0A7", "灵光翠青", "Yunara", "Yunara", "芸阿娜", "不破之誓", "永岚", "尤娜拉"),
        new ChampionTheme("Naafiri", "纳亚菲利", "#3AA8FF", "默认主题", "Naafiri", "Naafiri", "百裂冥犬", "纳亚菲利"),
        new ChampionTheme("Gnar", "纳尔", "#3AA8FF", "默认主题", "Gnar", "Gnar", "纳尔", "迷失之牙"),
        new ChampionTheme("Velkoz", "维克兹", "#3AA8FF", "默认主题", "Velkoz", "Vel'Koz", "Velkoz", "维克兹", "虚空之眼"),
        new ChampionTheme("Viktor", "维克托", "#E7AF45", "机械金", "Viktor", "Viktor", "机械先驱", "维克托"),
        new ChampionTheme("Veigar", "维迦", "#3AA8FF", "默认主题", "Veigar", "Veigar", "维迦", "邪恶小法师"),
        new ChampionTheme("Ekko", "艾克", "#3AA8FF", "默认主题", "Ekko", "Ekko", "时间刺客", "艾克"),
        new ChampionTheme("Anivia", "艾尼维亚", "#3AA8FF", "默认主题", "Anivia", "Anivia", "冰晶凤凰", "艾尼维亚"),
        new ChampionTheme("Ashe", "艾希", "#7FD8FF", "寒霜蓝", "Ashe", "Ashe", "寒冰射手", "艾希"),
        new ChampionTheme("Irelia", "艾瑞莉娅", "#3AA8FF", "默认主题", "Irelia", "Irelia", "刀锋舞者", "艾瑞莉娅"),
        new ChampionTheme("Ivern", "艾翁", "#3AA8FF", "默认主题", "Ivern", "Ivern", "翠神", "艾翁"),
        new ChampionTheme("Rell", "芮尔", "#3AA8FF", "默认主题", "Rell", "Rell", "芮尔", "镕铁少女"),
        new ChampionTheme("Maokai", "茂凯", "#3AA8FF", "默认主题", "Maokai", "Maokai", "扭曲树精", "茂凯"),
        new ChampionTheme("Lillia", "莉莉娅", "#3AA8FF", "默认主题", "Lillia", "Lillia", "含羞蓓蕾", "莉莉娅"),
        new ChampionTheme("Samira", "莎弥拉", "#3AA8FF", "默认主题", "Samira", "Samira", "沙漠玫瑰", "莎弥拉"),
        new ChampionTheme("Mordekaiser", "莫德凯撒", "#3AA8FF", "默认主题", "Mordekaiser", "Mordekaiser", "莫德凯撒", "铁铠冥魂"),
        new ChampionTheme("Morgana", "莫甘娜", "#6F43C8", "堕天紫", "Morgana", "Morgana", "堕落天使", "莫甘娜"),
        new ChampionTheme("Fizz", "菲兹", "#3AA8FF", "默认主题", "Fizz", "Fizz", "潮汐海灵", "菲兹"),
        new ChampionTheme("Fiora", "菲奥娜", "#3AA8FF", "默认主题", "Fiora", "Fiora", "无双剑姬", "菲奥娜"),
        new ChampionTheme("Seraphine", "萨勒芬妮", "#3AA8FF", "默认主题", "Seraphine", "Seraphine", "星籁歌姬", "萨勒芬妮"),
        new ChampionTheme("Shaco", "萨科", "#3AA8FF", "默认主题", "Shaco", "Shaco", "恶魔小丑", "萨科"),
        new ChampionTheme("DrMundo", "蒙多医生", "#3AA8FF", "默认主题", "DrMundo", "Dr. Mundo", "DrMundo", "祖安狂人", "蒙多", "蒙多医生"),
        new ChampionTheme("Vi", "蔚", "#3AA8FF", "默认主题", "Vi", "Vi", "皮城执法官", "蔚"),
        new ChampionTheme("Leona", "蕾欧娜", "#3AA8FF", "默认主题", "Leona", "Leona", "曙光女神", "蕾欧娜"),
        new ChampionTheme("Vex", "薇古丝", "#3AA8FF", "默认主题", "Vex", "Vex", "愁云使者", "薇古丝"),
        new ChampionTheme("Vayne", "薇恩", "#3AA8FF", "默认主题", "Vayne", "Vayne", "暗夜猎手", "薇恩"),
        new ChampionTheme("Nautilus", "诺提勒斯", "#3AA8FF", "默认主题", "Nautilus", "Nautilus", "深海泰坦", "诺提勒斯"),
        new ChampionTheme("Briar", "贝蕾亚", "#3AA8FF", "默认主题", "Briar", "Briar", "荆棘少女", "贝蕾亚"),
        new ChampionTheme("Fiddlesticks", "费德提克", "#3AA8FF", "默认主题", "Fiddlesticks", "Fiddlesticks", "末日使者", "费德提克"),
        new ChampionTheme("Jax", "贾克斯", "#3AA8FF", "默认主题", "Jax", "Jax", "武器大师", "贾克斯"),
        new ChampionTheme("Senna", "赛娜", "#3AA8FF", "默认主题", "Senna", "Senna", "涤魂圣枪", "赛娜"),
        new ChampionTheme("Sion", "赛恩", "#8A2E2E", "亡灵暗红", "Sion", "Sion", "亡灵战神", "赛恩"),
        new ChampionTheme("Hecarim", "赫卡里姆", "#3AA8FF", "默认主题", "Hecarim", "Hecarim", "战争之影", "赫卡里姆"),
        new ChampionTheme("XinZhao", "赵信", "#3AA8FF", "默认主题", "XinZhao", "Xin Zhao", "XinZhao", "德邦总管", "赵信"),
        new ChampionTheme("Singed", "辛吉德", "#3AA8FF", "默认主题", "Singed", "Singed", "炼金术士", "辛吉德"),
        new ChampionTheme("Syndra", "辛德拉", "#8E4BFF", "黑暗紫", "Syndra", "Syndra", "暗黑元首", "辛德拉"),
        new ChampionTheme("Janna", "迦娜", "#3AA8FF", "默认主题", "Janna", "Janna", "迦娜", "风暴之怒"),
        new ChampionTheme("Jinx", "金克丝", "#31D4C7", "狂想青", "Jinx", "Jinx", "暴走萝莉", "金克丝"),
        new ChampionTheme("Riven", "锐雯", "#3AA8FF", "默认主题", "Riven", "Riven", "放逐之刃", "锐雯"),
        new ChampionTheme("Thresh", "锤石", "#43D39E", "魂锁绿", "Thresh", "Thresh", "锤石", "魂锁典狱长"),
        new ChampionTheme("Akshan", "阿克尚", "#3AA8FF", "默认主题", "Akshan", "Akshan", "影哨", "阿克尚"),
        new ChampionTheme("Azir", "阿兹尔", "#3AA8FF", "默认主题", "Azir", "Azir", "沙漠皇帝", "阿兹尔"),
        new ChampionTheme("Alistar", "阿利斯塔", "#3AA8FF", "默认主题", "Alistar", "Alistar", "牛头酋长", "阿利斯塔"),
        new ChampionTheme("Akali", "阿卡丽", "#3AA8FF", "默认主题", "Akali", "Akali", "离群之刺", "阿卡丽"),
        new ChampionTheme("Amumu", "阿木木", "#3AA8FF", "默认主题", "Amumu", "Amumu", "殇之木乃伊", "阿木木"),
        new ChampionTheme("Ahri", "阿狸", "#FF6FAE", "魅惑粉", "Ahri", "Ahri", "九尾妖狐", "阿狸"),
        new ChampionTheme("Aurora", "阿萝拉", "#9B7CFF", "灵界紫", "Aurora", "Aurora", "双界灵兔", "阿萝拉"),
        new ChampionTheme("RekSai", "雷克赛", "#3AA8FF", "默认主题", "RekSai", "Rek'Sai", "RekSai", "虚空遁地兽", "雷克赛"),
        new ChampionTheme("Renekton", "雷克顿", "#3AA8FF", "默认主题", "Renekton", "Renekton", "荒漠屠夫", "雷克顿"),
        new ChampionTheme("Rengar", "雷恩加尔", "#3AA8FF", "默认主题", "Rengar", "Rengar", "傲之追猎者", "雷恩加尔"),
        new ChampionTheme("Xayah", "霞", "#3AA8FF", "默认主题", "Xayah", "Xayah", "逆羽", "霞"),
        new ChampionTheme("Varus", "韦鲁斯", "#3AA8FF", "默认主题", "Varus", "Varus", "惩戒之箭", "韦鲁斯"),
        new ChampionTheme("Nocturne", "魔腾", "#3AA8FF", "默认主题", "Nocturne", "Nocturne", "永恒梦魇", "魔腾"),
        new ChampionTheme("Heimerdinger", "黑默丁格", "#3AA8FF", "默认主题", "Heimerdinger", "Heimerdinger", "大发明家", "黑默丁格"),
    };

    private static readonly Dictionary<string, ChampionTheme> AliasLookup = BuildAliasLookup();

    public static string ResolveChampionHex(string? championName)
        => ResolveChampionTheme(championName)?.DefaultHex ?? "#3AA8FF";

    public static string ResolveChampionKey(string? championName)
        => ResolveChampionTheme(championName)?.ChampionKey ?? (string.IsNullOrWhiteSpace(championName) ? string.Empty : championName.Trim());

    public static string ResolveChampionDisplayName(string? championName)
        => ResolveChampionTheme(championName)?.DisplayName ?? (string.IsNullOrWhiteSpace(championName) ? "未识别英雄" : championName.Trim());

    public static IReadOnlyList<HeroThemeSettingSnapshot> CreateDefaultSettings(string? currentChampionName = null)
    {
        var comparer = CultureInfo.GetCultureInfo("zh-CN").CompareInfo;
        var list = ChampionThemes
            .OrderBy(x => x.DisplayName, Comparer<string>.Create((a, b) => comparer.Compare(a, b, CompareOptions.StringSort)))
            .Select(x => new HeroThemeSettingSnapshot(
                ChampionKey: x.ChampionKey,
                DisplayName: x.DisplayName,
                DefaultHex: x.DefaultHex,
                CurrentHex: x.DefaultHex,
                ThemeLabel: x.ThemeLabel,
                IconUrl: string.IsNullOrWhiteSpace(x.IconChampionId) ? null : $"{ChampionIconBase}{x.IconChampionId}.png",
                SearchText: string.Join(" ", x.Aliases.Append(x.DisplayName).Append(x.ChampionKey))))
            .ToList();

        if (!string.IsNullOrWhiteSpace(currentChampionName) && ResolveChampionTheme(currentChampionName) is null)
        {
            var normalized = currentChampionName.Trim();
            list.Insert(0, new HeroThemeSettingSnapshot(
                ChampionKey: normalized,
                DisplayName: normalized,
                DefaultHex: "#3AA8FF",
                CurrentHex: "#3AA8FF",
                ThemeLabel: "当前英雄待自定义",
                IconUrl: null,
                SearchText: normalized));
        }

        return list;
    }

    private static ChampionTheme? ResolveChampionTheme(string? championName)
    {
        var normalized = NormalizeKey(championName);
        return string.IsNullOrWhiteSpace(normalized) ? null : AliasLookup.GetValueOrDefault(normalized);
    }

    private static Dictionary<string, ChampionTheme> BuildAliasLookup()
    {
        var lookup = new Dictionary<string, ChampionTheme>(StringComparer.OrdinalIgnoreCase);
        foreach (var theme in ChampionThemes)
        {
            lookup[NormalizeKey(theme.ChampionKey)] = theme;
            lookup[NormalizeKey(theme.DisplayName)] = theme;
            foreach (var alias in theme.Aliases)
            {
                var normalized = NormalizeKey(alias);
                if (!string.IsNullOrWhiteSpace(normalized)) lookup[normalized] = theme;
            }
        }
        return lookup;
    }

    public static string NormalizeKey(string? championName)
        => string.IsNullOrWhiteSpace(championName)
            ? string.Empty
            : championName.Replace("'", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("·", string.Empty, StringComparison.Ordinal)
                .Replace("・", string.Empty, StringComparison.Ordinal)
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace("&", string.Empty, StringComparison.Ordinal)
                .Trim();
}