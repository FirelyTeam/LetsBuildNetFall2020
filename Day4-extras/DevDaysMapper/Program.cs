using CsvHelper;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;

namespace DevDaysMapper
{
    class Program
    {
        static void Main(string[] args)
        {

            TextReader reader = new StreamReader("sample-data-2.csv");
            var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csvReader.GetRecords<CSVModel>();

            var patientList = new List<Patient>();
            var observationList = new List<Observation>();

            var client = new FhirClient("https://vonk.fire.ly/r4");

            var map = new Mapper();

            // For day 4, we changed back to the initial code for mapping
            foreach (var r in records)
            {
                var pat = map.GetPatient(r);
                if (!patientList.Exists(p => p.Id == pat.Id))
                    patientList.Add(pat);


                observationList.Add(map.GetWhiteBloodCellCount(r));
                observationList.Add(map.GetRedBloodCellCount(r));
                observationList.Add(map.GetHemoglobin(r));
            }


            // Disabled this for day 4, to focus on the transaction output
            // DisplayData(patientList, observationList);

            // before adding the resources to the server, let's validate them first
            // ValidateResources(observationList.Take(10));

            CreateAndSendTransaction(patientList, observationList, client);

            /* 
             * Disabled the read of secured data for this exercise (day 3)
             * 
            var (securedPatientList, securedObservationList) = GetSecuredData();

            DisplayData(securedPatientList, securedObservationList);
            */

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void CreateAndSendTransaction(List<Patient> patients, List<Observation> observations, FhirClient client)
        {
            foreach (var p in patients)
            {
                var builder = new TransactionBuilder(client.Endpoint, Bundle.BundleType.Transaction);

                builder.Create(p);

                var patsObservations = observations.FindAll(o => o.Subject.Reference.Equals("urn:uuid:" + p.Id));

                foreach (var o in patsObservations)
                    builder.Create(o);

                // finalize details, setting the fullUrl of the patient entry, so references will be updated correctly
                var transactionBundle = builder.ToBundle();
                transactionBundle.Entry[0].FullUrl = "urn:uuid:" + p.Id;

                var response = client.Transaction(transactionBundle);

                // Let's write part of the response to our output
                var serializer = new FhirJsonSerializer();
                var createdPatient = serializer.SerializeToString(response.Entry[0]);
                var firstObservation = serializer.SerializeToString(response.Entry[1]);

                Console.WriteLine("Transaction response:");
                Console.WriteLine(createdPatient);
                Console.WriteLine();
                Console.WriteLine(firstObservation);
            }
        }

        private static void ValidateResources(IEnumerable<Observation> observationList)
        {
            var resolver = ZipSource.CreateValidationSource();
            var directoryResolver = new DirectorySource("profiles");

            var settings = ValidationSettings.CreateDefault();
            settings.ResourceResolver = new CachedResolver(
                new MultiResolver(resolver, directoryResolver));

            var validator = new Validator(settings);

            foreach (var obs in observationList)
            {
                // validate observation against standard profile (https://hl7.org/fhir/observation.html)
                var outcome = validator.Validate(obs);
                WriteOutcome(outcome, obs);

                // validate observation against custom profile (cardinality of category is 2..*)
                outcome = validator.Validate(obs, new[] { "http://fire.ly/fhir/StructureDefinition/MyObservation" });
                WriteOutcome(outcome, obs);
            }
        }

        private static void WriteOutcome(OperationOutcome outcome, Resource resource)
        {
            Console.WriteLine($"Validation of resource with id '{resource.Id}' {(outcome.Success ? "is successful" : "has failed:")}");
            if (!outcome.Success)
            {
                outcome.Issue.ForEach(i => Console.WriteLine($"  {i}"));
            }
        }

        private static (List<Patient> patients, List<Observation> observations) GetSecuredData()
        {
            var handler = new AuthorizationMessageHandler();
            var bearerToken = "AbCdEf123456"; //example-token;
            handler.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);


            var client = new FhirClient("https://labs.vonk.fire.ly/r4", messageHandler: handler);
            var pat = client.Read<Patient>("Patient/test");

            var patients = new List<Patient>();
            var observations = new List<Observation>();

            patients.Add(pat);

            // Search for the white blood cell count Observations based on LOINC code
            var q = new SearchParams("code", "http://loinc.org|6690-2");
            var result = client.Search<Observation>(q);

            foreach (var e in result.Entry)
            {
                observations.Add((Observation)e.Resource);
            }

            return (patients, observations);
        }

        static void DisplayData(List<Patient> patients, List<Observation> observations)
        {
            // This can be greatly improved, but just functions as an example of obtaining data from
            // a FHIR resource and displaying that


            Console.WriteLine($"There are {patients.Count} patients in the file. Here is their data:");

            foreach (var p in patients)
            {
                Console.WriteLine("----------------------------------------------------------------------------------------");

                Console.WriteLine("Data for patient: " + p.Name.First().Given.First() + " " + p.Name.First().Family + ", technical id: " + p.Id);

                var patsObservations = observations.FindAll(o => o.Subject.Reference.EndsWith("Patient/" + p.Id));

                // Split the observations based on type
                var wbcCode = new CodeableConcept("http://loinc.org", "6690-2");
                var rbcCode = new CodeableConcept("http://loinc.org", "789-8");
                var hbCode = new CodeableConcept("http://loinc.org", "718-7");

                var wbcObservations = patsObservations.FindAll(o => o.Code.Matches(wbcCode));
                var rbcObservations = patsObservations.FindAll(o => o.Code.Matches(rbcCode));
                var hbObservations = patsObservations.FindAll(o => o.Code.Matches(hbCode));

                Console.WriteLine("  WBC Observations:");
                foreach (var obs in wbcObservations)
                {
                    var qty = (Quantity)obs.Value;
                    Console.WriteLine($"    {obs.Effective}  {qty.Value} {qty.Code}");
                }
                Console.WriteLine("  RBC Observations:");
                foreach (var obs in rbcObservations)
                {
                    var qty = (Quantity)obs.Value;
                    Console.WriteLine($"    {obs.Effective}  {qty.Value} {qty.Code}");
                }
                Console.WriteLine("  HB Observations:");
                foreach (var obs in hbObservations)
                {
                    var qty = (Quantity)obs.Value;
                    Console.WriteLine($"    {obs.Effective}  {qty.Value} {qty.Code}");
                }
            }
            Console.WriteLine("-----------------------------------------------------------------------------------------");

        }
    }
}
