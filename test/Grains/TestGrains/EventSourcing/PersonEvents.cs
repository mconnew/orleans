using System;
using TestGrainInterfaces;

namespace TestGrains
{
    // We list all the events supported by the JournaledPersonGrain 

    // we chose to have all these events implement the following marker interface
    // (this is optional, but gives us a bit more typechecking)
    public interface IPersonEvent { } 

    [Serializable]
    [Hagar.GenerateSerializer]
    public class PersonRegistered : IPersonEvent
    {
        [Hagar.Id(0)]
        public string FirstName { get; set; }
        [Hagar.Id(1)]
        public string LastName { get; set; }
        [Hagar.Id(2)]
        public GenderType Gender { get; set; }

        public PersonRegistered(string firstName, string lastName, GenderType gender)
        {
            FirstName = firstName;
            LastName = lastName;
            Gender = gender;
        }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class PersonMarried : IPersonEvent
    {
        [Hagar.Id(0)]
        public Guid SpouseId { get; set; }
        [Hagar.Id(1)]
        public string SpouseFirstName { get; set; }
        [Hagar.Id(2)]
        public string SpouseLastName { get; set; }
        
        public PersonMarried(Guid spouseId, string spouseFirstName, string spouseLastName)
        {
            SpouseId = spouseId;
            SpouseFirstName = spouseFirstName;
            SpouseLastName = spouseLastName;
        }
    }

    [Serializable]
    [Hagar.GenerateSerializer]
    public class PersonLastNameChanged : IPersonEvent
    {
        [Hagar.Id(0)]
        public string LastName { get; set; }

        public PersonLastNameChanged(string lastName)
        {
            LastName = lastName;
        }
    }
}
