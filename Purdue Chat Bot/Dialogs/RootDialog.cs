using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Collections.Generic;

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

            var diningCourtMatch = Regex.Match(activity.Text, "dining court ([a-zA-Z]*)");

            if (diningCourtMatch.Success)
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync($"https://api.hfs.purdue.edu/menus/v1/locations/{diningCourtMatch.Groups[1]?.Value}/09-16-2017");
                    var content = await response.Content.ReadAsAsync<Menu>();

                    var responseText = string.Empty;
                    responseText += "For lunch there is:<br>";

                    foreach(var section in content.Lunch)
                    {
                        responseText += $"On the {section.Name} there is:<br>";

                        foreach(var item in section.Items)
                        {
                            responseText += $"{item.Name}";
                        }
                    }

                    await context.PostAsync(responseText);
                }
            }
            else
            {
                await context.PostAsync($"I don't recognize that.");
            }

            context.Wait(MessageReceivedAsync);
        }
    }

    [DataContract]
    public class Lunch
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
    public class Dinner
    {

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Items")]
        public IList<Item> Items { get; set; }
    }

    [DataContract]
    public class Menu
    {

        [DataMember(Name = "Breakfast")]
        public IList<object> Breakfast { get; set; }

        [DataMember(Name = "Lunch")]
        public IList<Lunch> Lunch { get; set; }

        [DataMember(Name = "Dinner")]
        public IList<Dinner> Dinner { get; set; }
    }
}