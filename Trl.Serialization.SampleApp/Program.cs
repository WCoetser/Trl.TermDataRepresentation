using System;
using System.Collections.Generic;

namespace Trl.Serialization.SampleApp
{
    class Program
    {
        const string INPUT_DESERIALIZE = @"
person1 => Person<Name,Born>(""Socrates"", -470);
person2 => Person<Name,Born>(""Plato"", -423);
person3 => Person<Name,Born>(""Aristotle"", -384);

root: (person1, person2, person3);
";

        static Person[] INPUT_SERIALIZE = new Person[]
        {
            new Person { Name = "Socrates", Born = -470 },
            new Person { Name = "Plato", Born = -423 },
            new Person { Name = "Aristotle", Born = -384 }
        };

        public static void Deserialize()
        {
            StringSerializer serializer = new StringSerializer();
            var philosophers = serializer.Deserialize<List<Person>>(INPUT_DESERIALIZE);
            Console.WriteLine("Deserialize ...");
            foreach (var p in philosophers)
            {
                Console.WriteLine($"Name = {p.Name}, Born = {p.Born}");
            }
            Console.WriteLine();
        }
        
        public static void Serialize()
        {           
            StringSerializer serializer = new StringSerializer();
            var philosophers = serializer.Serialize(INPUT_SERIALIZE);
            Console.WriteLine("Serialize ...");
            Console.WriteLine(philosophers);
            Console.WriteLine();
        }

        static void Main()
        {
            Serialize();
            Deserialize();
        }
    }
}
