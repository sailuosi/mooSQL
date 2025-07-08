


命名规范

word  代表词语，为SQL下的最小单元

clause 从句，为语句的主要组成部分，如select部分、from部分、where部分等

sentence  语句，一个能够独立执行的完整SQL语句。如 select /insert / update /delete /merge 等语句

phrase 词组，比 word大，又比从句小的部分。

bag  组合，比如select常用 逗号组合一组内容，如select的主要字段列表部分，order by的成员，group by 的成员

类结构

Clause   -- 所有SQL节点的根类

接口约束
IExpWord  -- 表达式接口，表达式优先级。