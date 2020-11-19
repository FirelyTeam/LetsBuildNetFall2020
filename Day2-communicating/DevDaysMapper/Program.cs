using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using CsvHelper;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;

namespace DevDaysMapper
{
    class Program
    {
        static void Main(string[] args)
        {

            TextReader reader = new StreamReader("sample-data.csv");
            var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csvReader.GetRecords<CSVModel>();

            var patientList = new List<Patient>();
            var observationList = new List<Observation>();

            var client = new FhirClient("https://vonk.fire.ly/r4");

            var map = new Mapper();
            foreach (var r in records)
            {
                var pat = map.GetPatient(r);
                if (!patientList.Exists(p => p.Identifier.First().Value == r.PATIENT_ID))
                {
                    var createdPat = client.Create<Patient>(pat);

                    // Adding the Patient with the server assigned technical id to the list
                    patientList.Add(createdPat);
                }

                // make sure we use the correct technical id in the Observations
                var technicalId = patientList.Find(p => p.Identifier.First().Value == r.PATIENT_ID).Id;

                observationList.Add(map.GetWhiteBloodCellCount(r, technicalId));
                observationList.Add(map.GetRedBloodCellCount(r, technicalId));
                observationList.Add(map.GetHemoglobin(r, technicalId));
            }

            foreach (var obs in observationList)
            {
                client.Create<Observation>(obs);
            }

            DisplayData(patientList, observationList);

            var (securedPatientList, securedObservationList) = GetSecuredData();

            DisplayData(securedPatientList, securedObservationList);
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

            foreach(var e in result.Entry)
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

                Console.WriteLine("Data for patient: "+ p.Name.First().Given.First() + " " + p.Name.First().Family + ", technical id: " + p.Id);

                var patsObservations = observations.FindAll(o=>o.Subject.Reference.EndsWith("Patient/" + p.Id));

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
