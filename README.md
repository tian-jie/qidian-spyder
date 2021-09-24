# qidian-spyder
尝试爬取起点中文网的所有小说，纯技术方案。目标是能够100%爬取起点的小说

# 问题
- 刚进来就发现一个起步就很困难的问题：所有起点的分页，全部被限制到5页以内了。。。
   - 针对这个问题，初步的想法是尽量细化各种分类搜索的条件
- VIP章节无法抓取，只能抓取前一小段
- 但发现另一个问题，所有的评论都是可以抓下来的。


# 初步方案
1. 从这个网站上开始：https://www.qidian.com/all/

然后可以发现，上面有非常多的分类：

  - 男女： 男生： https://www.qidian.com/all/， 女生： https://www.qidian.com/<B>mm</B>/all/
  - 分类：
    - 玄幻： https://www.qidian.com/all/<B>chanId21</B>/ 
      - 玄幻下一级分类，东方玄幻：https://www.qidian.com/all/chanId21<B>-subCateId8</B>/
      - 关键点：以下所有的组合
      ```
      data-typeid="21" data-subtypeid="8" 
      ```
    - 奇幻： https://www.qidian.com/all/chanId1/

    - chanId21
      - subCateId: 8, 78, 58, 73
    - chanId1
      - subCateId: 38, 62, 201, 202, 20092, 20093
    - chanId2
      - subCateId: 5, 30, 206, 20099, 20100
    - chanId22
      - subCateId: 18, 44, 64, 207, 20101

    - chanId4
      - subCateId:12, 16,74,130,151,153
    - chanId15
      - subCateId:20104,20105,20106,20107,20108,6,209
    - chanId6
      - subCateId:54,65,80,230,231
    - chanId5
      - subCateId:22,48,220,32,222,223,224,225,226,20094
    - chanId7
      - subCateId:7,70,240,20102,20103

    - chanId8
      - subCateId:28,55,82
    - chanId9
      - subCateId:21,25,68,250,251,252,253
    - chanId10
      - subCateId:26,35,57,260,20095
    - chanId12
      - subCateId:60,66,281,282
    - chanId20076
      - subCateId:20097,20098,20075,20077,20078,20079,20096


  - 状态：
    - 连载：action0
    - 完本：action1
  - 属性：
    - 免费：VIP0
    - VIP: VIP1
  - 字数：
    - size1 ~ size5
  - 品质：
    - sign1, sign2
  - 更新时间：
    - update1 ~ update4
  - 标签：
    - tagXXX（如：tag豪门，tag穿越等）
    - tag豪门 tag孤儿 tag盗贼 tag特工 tag黑客 tag明星 tag特种兵 tag杀手 tag老师 tag学生 tag胖子 tag宠物 tag蜀山 tag魔王附体 tagLOL tag废材流 tag护短 tag卡片 tag手游 tag法师 tag医生 tag感情 tag鉴宝 tag亡灵 tag职场 tag吸血鬼 tag龙 tag西游 tag鬼怪 tag阵法 tag魔兽 tag勇猛 tag玄学 tag群穿 tag丹药 tag练功流 tag召唤流 tag恶搞 tag爆笑 tag轻松 tag冷酷 tag腹黑 tag阳光 tag狡猾 tag机智 tag猥琐 tag嚣张 tag淡定 tag僵尸 tag丧尸 tag盗墓 tag随身流 tag软饭流 tag无敌文 tag异兽流 tag系统流 tag洪荒流 tag学院流 tag位面 tag铁血 tag励志 tag坚毅 tag变身 tag强者回归 tag赚钱 tag争霸流 tag种田文 tag宅男 tag无限流 tag技术流 tag凡人流 tag热血 tag重生 tag穿越


以上多个条件混合的时候，除了男女这个选项外，其他选项全部在最后那个参数上用减号隔开。
如：男，东方玄幻，连载： https://www.qidian.com/all/chanId21-subCateId8-action0/
chanId和subCateId有相应的组合

经过分析，以上内容回头先人工抓取下来，作为入口地址。


# 系统架构
- Redis: 放所有爬过的链接，用以快速判断，这个链接是否已经爬过，如果爬过，就不放到RabbitMQ里了，如果没爬过才放到RabbitMQ里，提高效率，防止重复爬取；
- RabbitMQ: 有多个不同的队列：
  - 各种页面，可以从这些页面上爬到小说入口
  - 小说入口，可以爬到这个小说的所有章节
  - 具体章节页面，可以爬到小说内容
  - 根据某个小说的章节，可以爬到相对应的评论
- RDBMS：存两方面内容
  - 小说相关
    - 小说的具体内容，包括小说的标题、作者等基本信息，以及小说的不同章节的纯文本内容；
    - 小说的评论相关内容
  - 爬取的内容相关
    - 所有爬取的网页，都需要hardcopy存到本地一份；
- 入口解析器: docker，可以多开，从杂乱无章的页面上，分析出小说的入口页面链接
- 章节解析器: docker，可以多开，从小说入口页面，分析出小说的章节页面链接
- 内容解析器: docker，可以多开，从小说的章节页面链接，解析出小说正文内容
- 评论解析器: docker，可以多开，根据小说的章节页面链接，拼凑出的评论页面，解析出评论内容

## 整体架构
![image](https://user-images.githubusercontent.com/25471485/134043003-13d1f67d-d2ea-463e-9973-0954ca379d5b.png)

## 详细流程图
![image](https://user-images.githubusercontent.com/25471485/134043158-6bc0fd4b-678c-4e98-81d2-a3f4611b4919.png)

