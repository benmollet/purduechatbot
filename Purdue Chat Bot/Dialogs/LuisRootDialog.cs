using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Purdue_Chat_Bot.Dialogs
{
    [LuisModel("<Id>", "<password>")]
    [Serializable]
    public class LuisRootDialog : LuisDialog<object>
    {
        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";

            await context.PostAsync(message);

            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Get Menu")]
        public async Task Search(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;

            EntityRecommendation diningCourt;
            result.TryFindEntity("Dining Court", out diningCourt);

            if (diningCourt != null)
            {
                var knownDiningCourts = new List<string>()
                {
                    "earhart",
                    "ford",
                    "wiley",
                    "hillenbrand",
                    "windsor"
                };

                if (!knownDiningCourts.Contains(diningCourt.Entity.ToLower()))
                {
                    await context.PostAsync($"I'm sorry I don't know a dining court named {diningCourt.Entity}");
                }
                else
                {
                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync($"https://api.hfs.purdue.edu/menus/v1/locations/{diningCourt.Entity}/09-16-2017");
                        var menu = await response.Content.ReadAsAsync<Menu>();

                        var textResponse = $"For Breakfast there is:<br>{this.ReadMeal(menu.Breakfast)}<br>";
                        textResponse += $"For Lunch there is:<br>{this.ReadMeal(menu.Lunch)}<br>";
                        textResponse += $"For Dinner there is:<br>{this.ReadMeal(menu.Dinner)}";

                        await context.PostAsync(textResponse);
                    }
                }
            }
            else
            {
                await context.PostAsync("You didn't ask me about a dining court");
            }
        }

        public string ReadMeal(IList<MenuSection> menuSections)
        {
            var textResponse = string.Empty;

            foreach (var menuSection in menuSections)
            {
                textResponse += $"On the {menuSection.Name} there is:<br>";

                foreach (var menuItem in menuSection.Items)
                {
                    textResponse += $"{menuItem.Name}<br>";
                }
            }

            return textResponse;
        }
    }
}