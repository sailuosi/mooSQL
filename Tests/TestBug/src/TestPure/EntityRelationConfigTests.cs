using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using mooSQL.data;
using mooSQL.data.Mapping;
using mooSQL.Pure.Tests.TestHelpers;
using Xunit;

namespace mooSQL.Pure.Tests
{
    /// <summary>Fluent configureEntity / Relation → EntityNavi 绑定。</summary>
    public class EntityRelationConfigTests
    {
        static MooClient CreateClient()
        {
            var client = new MooClient();
            var factory = new BaseEntityAnalyseFactory();
            factory.register(new MooEntityAnalyser());
            client.entityAnalyseFactory = factory;
            return client;
        }

        [Fact]
        public void ConfigureEntity_Relation_WritesNavigat_OnCollectionAndReverse()
        {
            var client = CreateClient();
            client.configureEntity<RelBlog>(p =>
            {
                p.Relation<RelPost>((a, b) => a.Id == b.BlogId);
            });

            var blog = client.EntityCash.getEntityInfo<RelBlog>();
            var postsCol = blog.GetColumn(nameof(RelBlog.Posts));
            postsCol.Should().NotBeNull();
            postsCol.Navigat.Should().NotBeNull();
            postsCol.Navigat.BossKey.Should().Be(nameof(RelBlog.Id));
            postsCol.Navigat.SlaveKey.Should().Be(nameof(RelPost.BlogId));
            postsCol.Navigat.ChildType.Should().Be(typeof(RelPost));
            postsCol.Navigat.NavigatType.Should().Be(EnityNaviType.OneToMany);

            var post = client.EntityCash.getEntityInfo<RelPost>();
            var blogNav = post.GetColumn(nameof(RelPost.Blog));
            blogNav.Should().NotBeNull();
            blogNav.Navigat.Should().NotBeNull();
            blogNav.Navigat.BossKey.Should().Be(nameof(RelPost.BlogId));
            blogNav.Navigat.SlaveKey.Should().Be(nameof(RelBlog.Id));
            blogNav.Navigat.NavigatType.Should().Be(EnityNaviType.ManyToOne);
        }

        [Fact]
        public void ConfigureEntity_Relation_ManyToOne_BossKeyIsFkOnParent()
        {
            var client = CreateClient();
            client.configureEntity<RelBlog>(p =>
            {
                p.Relation<RelBlogUser>((a, b) => a.UserId == b.Id);
            });

            var blog = client.EntityCash.getEntityInfo<RelBlog>();
            var userNav = blog.GetColumn(nameof(RelBlog.BlogUser));
            userNav.Navigat.BossKey.Should().Be(nameof(RelBlog.UserId));
            userNav.Navigat.SlaveKey.Should().Be(nameof(RelBlogUser.Id));
            userNav.Navigat.NavigatType.Should().Be(EnityNaviType.ManyToOne);
        }

        [Fact]
        public void Relation_InvalidLambda_Throws()
        {
            var client = CreateClient();
            Action act = () => client.configureEntity<RelBlog>(p =>
            {
                p.Relation<RelPost>((a, b) => a.Id == b.BlogId && a.UserId == b.BlogId);
            });
            act.Should().Throw<ArgumentException>().WithMessage("*等值*");
        }

        [Fact]
        public void Relation_AmbiguousNav_RequiresDisambiguation()
        {
            var client = CreateClient();
            Action act = () => client.configureEntity<RelAmbiguous>(p =>
            {
                p.Relation<RelPost>((a, b) => a.Id == b.BlogId);
            });
            act.Should().Throw<InvalidOperationException>().WithMessage("*多个*");
        }

        [Fact]
        public void Relation_Disambiguation_BindsNamedNav()
        {
            var client = CreateClient();
            client.configureEntity<RelAmbiguous>(p =>
            {
                p.Relation<RelPost>(x => x.PostsA, (a, b) => a.Id == b.BlogId);
            });

            var en = client.EntityCash.getEntityInfo<RelAmbiguous>();
            en.GetColumn(nameof(RelAmbiguous.PostsA)).Navigat.Should().NotBeNull();
            en.GetColumn(nameof(RelAmbiguous.PostsA)).Navigat.SlaveKey.Should().Be(nameof(RelPost.BlogId));
            // PostsB 未消歧绑定时仍可为空
            var b = en.GetColumn(nameof(RelAmbiguous.PostsB));
            (b == null || b.Navigat == null).Should().BeTrue();
        }

        [Fact]
        public void TwoClients_RelationRegistries_Isolated()
        {
            var c1 = CreateClient();
            var c2 = CreateClient();
            c1.configureEntity<RelBlog>(p => p.Relation<RelPost>((a, b) => a.Id == b.BlogId));

            c1.EntityCash.Relations.Find(typeof(RelBlog), typeof(RelPost)).Should().NotBeNull();
            c2.EntityCash.Relations.Find(typeof(RelBlog), typeof(RelPost)).Should().BeNull();
        }

        [Fact]
        public void IncludeNav_AfterConfigure_LoadsChildren()
        {
            using var fx = new SQLiteTestFixture();
            fx.ExecuteSql(@"
CREATE TABLE RelBlog (
  Id TEXT PRIMARY KEY,
  UserId TEXT,
  Url TEXT
);
CREATE TABLE RelPost (
  Id TEXT PRIMARY KEY,
  BlogId TEXT,
  Title TEXT
);");
            fx.ExecuteSql("INSERT INTO RelBlog (Id, UserId, Url) VALUES ('b1', 'u1', 'http://a');");
            fx.ExecuteSql("INSERT INTO RelPost (Id, BlogId, Title) VALUES ('p1', 'b1', 't1'), ('p2', 'b1', 't2'), ('p3', 'b2', 't3');");

            fx.Db.client.configureEntity<RelBlog>(p =>
            {
                p.Relation<RelPost>((a, b) => a.Id == b.BlogId);
            });

            List<RelBlog> blogs;
            using (var kit = TestDatabaseHelper.UseSQL(fx.Db))
            {
                blogs = kit.select("*").from("RelBlog").where("Id", "b1").query<RelBlog>().ToList();
            }
            blogs.Should().HaveCount(1);
            blogs[0].Posts = new List<RelPost>();

            using (var kit = TestDatabaseHelper.UseSQL(fx.Db))
            {
                kit.includeNav(blogs, b => b.Posts);
            }

            blogs[0].Posts.Should().HaveCount(2);
            blogs[0].Posts.Select(x => x.Id).Should().BeEquivalentTo(new[] { "p1", "p2" });
        }
    }

    [SooTable("RelBlog")]
    public class RelBlog
    {
        [SooColumn("Id", IsPrimaryKey = true)]
        public string Id { get; set; }

        [SooColumn("UserId")]
        public string UserId { get; set; }

        [SooColumn("Url")]
        public string Url { get; set; }

        public RelBlogUser BlogUser { get; set; }

        public List<RelPost> Posts { get; set; } = new List<RelPost>();
    }

    [SooTable("RelBlogUser")]
    public class RelBlogUser
    {
        [SooColumn("Id", IsPrimaryKey = true)]
        public string Id { get; set; }

        [SooColumn("Name")]
        public string Name { get; set; }
    }

    [SooTable("RelPost")]
    public class RelPost
    {
        [SooColumn("Id", IsPrimaryKey = true)]
        public string Id { get; set; }

        [SooColumn("BlogId")]
        public string BlogId { get; set; }

        [SooColumn("Title")]
        public string Title { get; set; }

        public RelBlog Blog { get; set; }
    }

    [SooTable("RelAmbiguous")]
    public class RelAmbiguous
    {
        [SooColumn("Id", IsPrimaryKey = true)]
        public string Id { get; set; }

        public List<RelPost> PostsA { get; set; } = new List<RelPost>();
        public List<RelPost> PostsB { get; set; } = new List<RelPost>();
    }
}
