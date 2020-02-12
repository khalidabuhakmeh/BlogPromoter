using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.Extensions.Configuration;
using TweetSharp;

namespace BlogPromoter
{
    class Program
    {
        private static readonly IConfiguration Configuration =
            new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

        private static readonly string[] Exclamations = new[]
        {
            "Check it out!",
            "Great post!",
            "Interesting post.",
            "I wrote",
            "ICYMI"
        };

        static void Main(string[] args)
        {
            var url = Configuration["blog_feed_url"];
            var twitter = Configuration["twitter_account"];

            using var reader = XmlReader.Create(url);
            var feed = SyndicationFeed.Load(reader);

            var post = feed
                .Items 
                // don't promote anything labeled general
                .Where(x => !x.Categories.Any(c => c.Label.Contains("general", StringComparison.OrdinalIgnoreCase)))
                // randomly order my posts
                .OrderBy(x => Guid.NewGuid())
                .FirstOrDefault();

            var exclamation = Exclamations
                .OrderBy(x => Guid.NewGuid())
                .First();

            var hashTags = string.Join(
                    " ",
                    post
                        .Categories
                        .Select(x => x.Name)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => $"#{x.Replace(",", string.Empty)}")
                )
                .Trim();
            
            var status =
                $"{exclamation} \"{post.Title.Text}\" ({post.PublishDate.Date.ToShortDateString()}) by {twitter} {hashTags} RTs appreciated. {post.Links[0].Uri} ({DateTime.Now.ToShortDateString()})";

            var service = new TwitterService(
                Configuration["twitter_consumer_key"],
                Configuration["twitter_consumer_secret"]
            );
            
            service.AuthenticateWith(
                Configuration["twitter_access_token"], 
                Configuration["twitter_access_token_secret"]
            );

            var result = service.SendTweet(new SendTweetOptions
            {
                Status = status
            });
            
            Console.WriteLine($"Successfully tweeted {result.IdStr}!\r\n{result.Text}");
        }
    }
}