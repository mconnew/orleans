using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using TestGrainInterfaces;

namespace TestGrains
{
    /// <summary>
    /// all chat events implement this interface, to define how each event changes the XML document
    /// </summary>
    public interface IChatEvent
    {
        void Update(XDocument document);
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class CreatedEvent : IChatEvent
    {
        [Hagar.Id(0)]
        public DateTime Timestamp { get; set; }
        [Hagar.Id(1)]
        public string Origin { get; set; }

        public void Update(XDocument document)
        {
            document.Initialize(Timestamp, Origin);
        }
    }


    [Serializable]
    [Hagar.GenerateSerializer]
    public class PostedEvent : IChatEvent
    {
        [Hagar.Id(0)]
        public Guid Guid { get; set; }
        [Hagar.Id(1)]
        public string User { get; set; }
        [Hagar.Id(2)]
        public DateTime Timestamp { get; set; }
        [Hagar.Id(3)]
        public string Text { get; set; }

        public void Update(XDocument document)
        {
            var container = document.GetPostsContainer();
            container.Add(ChatFormat.MakePost(Guid, User, Timestamp, Text));
            document.EnforceLimit();
        }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class DeletedEvent : IChatEvent
    {
        [Hagar.Id(0)]
        public Guid Guid { get; set; }

        public void Update(XDocument document)
        {
            document.FindPost(Guid.ToString())?.Remove();
        }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class EditedEvent : IChatEvent
    {
        [Hagar.Id(0)]
        public Guid Guid { get; set; }
        [Hagar.Id(1)]
        public string Text { get; set; }

        public void Update(XDocument document)
        {
            document.FindPost(Guid.ToString())?.ReplaceText(Text);
        }
    }
}
   