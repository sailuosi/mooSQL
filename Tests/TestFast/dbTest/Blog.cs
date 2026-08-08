using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dbTest
{
    public class Blog
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        public string UserId { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(UserId))]
        [FreeSql.DataAnnotations.Navigate(nameof(UserId))]
        [Chloe.Annotations.Navigation(nameof(UserId))]
        [Fast.Framework.Attributes.Navigate(MainName = nameof(UserId), ChildName = nameof(Id))]
        public virtual BlogUser BlogUser { get; set; }

        public string Url { get; set; }

        [Navigate(NavigateType.OneToMany, nameof(Post.BlogId))]
        [FreeSql.DataAnnotations.Navigate(nameof(Post.BlogId))]
        [Chloe.Annotations.Navigation]
        [Fast.Framework.Attributes. Navigate(MainName = nameof(Id), ChildName = nameof(Post.BlogId))]
        public virtual List<Post> Posts { get; set; }

        [Navigate(NavigateType.OneToMany, nameof(BlogTag.BlogId))]
        [FreeSql.DataAnnotations.Navigate(nameof(BlogTag.BlogId))]
        [Chloe.Annotations.Navigation]
        [Fast.Framework.Attributes.Navigate(MainName = nameof(Id), ChildName = nameof(BlogTag.BlogId))]
        public virtual List<BlogTag> Tags { get; set; }
    }
    public class BlogUser
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Post
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string BlogId { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(BlogId))]
        [FreeSql.DataAnnotations.Navigate(nameof(BlogId))]
        [Chloe.Annotations.Navigation(nameof(BlogId))]
        [Fast.Framework.Attributes.Navigate(MainName = nameof(BlogId), ChildName = nameof(Id))]
        public virtual Blog Blog { get; set; }

        public string UserId { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(UserId))]
        [FreeSql.DataAnnotations.Navigate(nameof(UserId))]
        [Chloe.Annotations.Navigation(nameof(UserId))]
        public virtual BlogUser BlogUser { get; set; }
    }
    public class BlogTag
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        public string BlogId { get; set; }

        [Chloe.Annotations.Navigation(nameof(BlogId))]//chloe必须?
        [SqlSugar.SugarColumn(IsIgnore = true)]
        [CRL.Data.Attribute.Field(MapingField = false)]
        [Fast.Framework.Attributes.Navigate(MainName = nameof(BlogId), ChildName = nameof(Id))]
        public virtual Blog Blog { get; set; }
        public string Tag { get; set; }
    }
}
