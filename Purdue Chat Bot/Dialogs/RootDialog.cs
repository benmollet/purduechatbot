using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Runtime.Serialization;

namespace Purdue_Chat_Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var readInDiningCourt = Regex.Match(activity.Text, @"dining court ([a-zA-Z]*)");
            var knownDiningCourts = new List<string>()
            {
                "Earhart"
            };

            if (!readInDiningCourt.Success)
            {
                await context.PostAsync("I'm sorry I don't know what you're asking me");
            }
            else if (!knownDiningCourts.Contains(readInDiningCourt.Groups[1].Value))
            {
                await context.PostAsync($"I'm sorry I don't know a dining court named {readInDiningCourt.Groups[1].Value}");
            }
            else
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync("https://api.hfs.purdue.edu/menus/v1/locations/Earhart/09-15-2017");
                    var menu = await response.Content.ReadAsAsync<Menu>();

                    var textResponse = $"For Breakfast there is:<br><hr>{this.ReadMeal(menu.Breakfast)}<br>";
                    textResponse += $"For Lunch there is:<br><hr>{this.ReadMeal(menu.Lunch)}<br>";
                    textResponse += $"For Dinner there is:<br><hr>{this.ReadMeal(menu.Dinner)}";

                    await context.PostAsync(textResponse);
                }
            }

            // return our reply to the user
            context.Wait(MessageReceivedAsync);
        }

        public string ReadMeal(IList<MenuSection> menuSections)
        {
            var textResponse = string.Empty;

            foreach (var menuSection in menuSections)
            {
                textResponse += $"On the {menuSection.Name} there is:<br><ul>";

                foreach (var menuItem in menuSection.Items)
                {
                    textResponse += $"<li>{menuItem.Name}</li><br>";
                }

                textResponse += "</ul>";
            }

            return textResponse;
        }
    }

    [DataContract]
    public class MenuSection
    {

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Items")]
        public IList<Item> Items { get; set; }
    }

    [DataContract]
    public class Item
    {

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "IsVegetarian")]
        public bool IsVegetarian { get; set; }
    }

    [DataContract]
    public class Menu
    {

        [DataMember(Name = "Breakfast")]
        public IList<MenuSection> Breakfast { get; set; }

        [DataMember(Name = "Lunch")]
        public IList<MenuSection> Lunch { get; set; }

        [DataMember(Name = "Dinner")]
        public IList<MenuSection> Dinner { get; set; }
    }
}