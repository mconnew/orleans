using System;
using Orleans;
using Orleans.EventSourcing;
using TestGrainInterfaces;

namespace TestGrains
{
    [Serializable]
    [Hagar.GenerateSerializer]
    public class PersonState
    {
        [Hagar.Id(0)]
        public string FirstName { get; set; }
        [Hagar.Id(1)]
        public string LastName { get; set; }
        [Hagar.Id(2)]
        public GenderType Gender { get; set; }
        [Hagar.Id(3)]
        public bool IsMarried { get; set; }

        public void Apply(PersonRegistered @event)
        {
            this.FirstName = @event.FirstName;
            this.LastName = @event.LastName;
            this.Gender = @event.Gender;
        }

        public void Apply(PersonMarried @event)
        {
            this.IsMarried = true;
        }

        public void Apply(PersonLastNameChanged @event)
        {
            this.LastName = @event.LastName;
        }
    }
}
