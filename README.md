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
    - 奇幻： https://www.qidian.com/all/chanId1/
  - 状态：
    - 连载：https://www.qidian.com/all/action0/，完本：https://www.qidian.com/all/action1/


以上多个条件混合的时候，除了男女这个选项外，其他选项全部在最后那个参数上用减号隔开。
如：男，东方玄幻，连载： https://www.qidian.com/all/chanId21-subCateId8-action0/

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
- 入口解析器: 从杂乱无章的页面上，分析出小说的入口页面链接
- 章节解析器: 从小说入口页面，分析出小说的章节页面链接
- 内容解析器: 从小说的章节页面链接，解析出小说正文内容
- 评论解析器: 根据小说的章节页面链接，拼凑出的评论页面，解析出评论内容

```mermaid
graph TD
  style Redis-A fill:#600,stroke:#333,stroke-width:4px; 
  style Redis-B fill:#600,stroke:#333,stroke-width:4px; 
  style Redis-C fill:#600,stroke:#333,stroke-width:4px; 
  style Redis-D fill:#600,stroke:#333,stroke-width:4px; 
  style RabbitMQ-A fill:#600,stroke:#333,stroke-width:4px; 
  style RabbitMQ-B fill:#600,stroke:#333,stroke-width:4px; 
  style RabbitMQ-C fill:#600,stroke:#333,stroke-width:4px; 
  style RabbitMQ-D fill:#600,stroke:#333,stroke-width:4px; 
  style 入口解析器 fill:#060,stroke:#333,stroke-width:4px; 
  style 章节解析器 fill:#060,stroke:#333,stroke-width:4px; 
  style 正文解析器 fill:#060,stroke:#333,stroke-width:4px; 
  style 评论解析器 fill:#060,stroke:#333,stroke-width:4px; 
  style RDBMS-A fill:#600,stroke:#333,stroke-width:4px; 
  style RDBMS-B fill:#600,stroke:#333,stroke-width:4px; 
  style RDBMS-C fill:#600,stroke:#333,stroke-width:4px; 
  style RDBMS-D fill:#600,stroke:#333,stroke-width:4px; 

  style Redis-A判断 fill:#066,stroke:#333,stroke-width:4px; 
  style Redis-B判断 fill:#066,stroke:#333,stroke-width:4px; 
  style Redis-C判断 fill:#066,stroke:#333,stroke-width:4px; 
  style Redis-D判断 fill:#066,stroke:#333,stroke-width:4px; 

  Init[手动初始化入口页面] --> Redis-A("Redis<br>小说分类页面链接")
  Init --> RabbitMQ-A("RabbitMQ<br>小说分类网页链接")

  subgraph 入口解析 子系统
    RabbitMQ-A --> |取出一个分类页面链接| 入口解析器(入口分析器)
    入口解析器 --> |抓取页面, 从页面上分析<br>有哪些其他分类<br>找出其他分类页的链接| Redis-A判断{Redis里是否有<br>这个链接}
    Redis-A判断 --> |有| 丢弃-A((丢弃))
    Redis-A判断 --> |无| Redis-A
    Redis-A --> |同时保存到RabbitMQ中| RabbitMQ-A
    入口解析器 --> |抓取页面<br>这个页面的内容| RDBMS-A["RDBMS<BR> 小说分类页面html"]
  end

  subgraph 章节解析器 子系统

    RabbitMQ-B --> |取出一个小说入口链接| 章节解析器(章节解析器)
    Redis-B判断 --> |有| 丢弃-B((丢弃))
    Redis-B判断 --> |无| Redis-B("Redis<br>章节链接")
    Redis-B --> |同时保存到RabbitMQ中| RabbitMQ-B("RabbitMQ<br>小说章节链接")
    入口解析器 --> |抓取页面, 从页面上分析<br>有哪些小说<br>找出小说入口页的链接| Redis-B判断{Redis里是否有<br>这个链接} 
    章节解析器 --> |抓取页面<br>个页面的内容| RDBMS-B["RDBMS<BR> 章节网页"]
 
  end

  subgraph 正文解析器 子系统 
    RabbitMQ-C --> |取出一个小说章节链接| 正文解析器(正文解析器)
    正文解析器 --> |正文网页| RDBMS-C["RDBMS<BR> 正文内容"]
    正文解析器 --> |这一章小说的txt内容| RDBMS-C

    章节解析器 --> |抓取页面<br>从页面上分析小说的<br>所有章节链接<br>拼凑出评论连接| Redis-C判断{Redis里是否有<br>这个链接}
    Redis-C判断 --> |有| 丢弃-C((丢弃))
    Redis-C判断 --> |无| Redis-C
    Redis-C --> |同时保存到RabbitMQ中| RabbitMQ-C

  end 

  subgraph 评论解析器 子系统 
    RabbitMQ-D --> |取出一个小说章节评论链接| 评论解析器(评论解析器)
    评论解析器 --> |评论html| RDBMS-D["RDBMS<BR> 正文内容"]
    评论解析器 --> |评论结构化数据| RDBMS-D

    章节解析器 --> |抓取页面<br>从页面上分析小说的<br>所有章节链接<br>拼凑出评论连接| Redis-D判断{Redis里是否有<br>这个链接}
    Redis-D判断 --> |有| 丢弃-D((丢弃))
    Redis-D判断 --> |无| Redis-D
    Redis-D --> |同时保存到RabbitMQ中| RabbitMQ-D

  end 
```
