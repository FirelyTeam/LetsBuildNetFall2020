using System;
using Hl7.Fhir.Model;

namespace DevDaysMapper
{
    public class Mapper
    {
        public Mapper()
        {
        }

        public Patient GetPatient(CSVModel record)
        {
            Patient patient = new Patient();

            // Changed this for day 2, to store the ID in the CSV as identifier in the Patient resource
            // This way we do not lose the information, and also will be able to have the server assign an id
            patient.Identifier.Add(new Identifier("http://acme.org/patient-ids",record.PATIENT_ID));

            // Check your data first, and change this code if you have more options or null values for gender
            patient.Gender = (record.PATIENT_GENDER == "F") ? AdministrativeGender.Female : AdministrativeGender.Male;

            patient.Name.Add(new HumanName().WithGiven(record.PATIENT_GIVENNAME).AndFamily(record.PATIENT_FAMILYNAME));
            // This could also be accomplised without using fluent methods:
            //var name = new HumanName();
            //name.GivenElement.Add(new FhirString(record.PATIENT_GIVENNAME)); // given names go into a list
            //name.Family = record.PATIENT_FAMILYNAME;
            //patient.Name.Add(name);

            return patient;
        }

        public Observation GetWhiteBloodCellCount(CSVModel record, string id)
        {
            // White blood cell count - This corresponds to LOINC code:
            // Code:        6690-2
            // Display:     Leukocytes [#/volume] in Blood by Automated count
            // Unit System: http://unitsofmeasure.org
            // Unit Code:   10*3/uL

            Observation wbc = new Observation();

            wbc.Code = new CodeableConcept("http://loinc.org", "6690-2", "Leukocytes [#/volume] in Blood by Automated count");

            wbc.Value = new Quantity(decimal.Parse(record.WBC), "10*3/uL");

            // Changed this for day 2, now using the id parameter added to this method
            wbc.Subject = new ResourceReference("Patient/" + id);

            wbc.Identifier.Add(new Identifier("http://acme.org/sequence-nos", record.SEQN));

            wbc.Effective = new FhirDateTime(record.TIMESTAMP);

            // Mandatory field, set to 'final' since this comes from a completed dataset
            wbc.Status = ObservationStatus.Final;

            // Optionally add the category
            wbc.Category.Add(new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category", "laboratory", "Laboratory"));

            return wbc;
        }

        public Observation GetRedBloodCellCount(CSVModel record, string id)
        {
            // Red blood cell count - This corresponds to LOINC code:
            // Code:        789-8
            // Display:     Erythrocytes [#/volume] in Blood by Automated count
            // Unit System: http://unitsofmeasure.org
            // Unit Code:   10*6/uL

            Observation rbc = new Observation();

            rbc.Code = new CodeableConcept("http://loinc.org", "789-8", "Erythrocytes [#/volume] in Blood by Automated count");

            rbc.Value = new Quantity(decimal.Parse(record.RBC), "10*6/uL");

            // Changed this for day 2, now using the id parameter added to this method
            rbc.Subject = new ResourceReference("Patient/" + id);

            rbc.Identifier.Add(new Identifier("http://acme.org/sequence-nos", record.SEQN));

            rbc.Effective = new FhirDateTime(record.TIMESTAMP);

            // Mandatory field, set to 'final' since this comes from a completed dataset
            rbc.Status = ObservationStatus.Final;

            // Optionally add the category
            rbc.Category.Add(new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category", "laboratory", "Laboratory"));

            return rbc;
        }

        public Observation GetHemoglobin(CSVModel record, string id)
        {
            // Hemoglobin
            // Code:        718-7
            // Display:     Hemoglobin [Mass/volume] in Blood
            // Unit System: http://unitsofmeasure.org
            // Unit Code:   g/dL

            Observation hb = new Observation();

            hb.Code = new CodeableConcept("http://loinc.org", "718-7", "Hemoglobin [Mass/volume] in Blood");

            hb.Value = new Quantity(decimal.Parse(record.HB), "g/dL");

            // Changed this for day 2, now using the id parameter added to this method
            hb.Subject = new ResourceReference("Patient/" + id);

            hb.Identifier.Add(new Identifier("http://acme.org/sequence-nos", record.SEQN));

            hb.Effective = new FhirDateTime(record.TIMESTAMP);

            // Mandatory field, set to 'final' since this comes from a completed dataset
            hb.Status = ObservationStatus.Final;

            // Optionally add the category
            hb.Category.Add(new CodeableConcept("http://terminology.hl7.org/CodeSystem/observation-category", "laboratory", "Laboratory"));

            return hb;
        }
    }
}
