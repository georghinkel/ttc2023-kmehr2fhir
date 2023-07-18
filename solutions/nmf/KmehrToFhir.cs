using Hsrm.TTC23.Fhir;
using Hsrm.TTC23.Kmehr;
using nmf;
using NMF.Collections.ObjectModel;
using NMF.Expressions;
using NMF.Expressions.Linq;
using NMF.Synchronizations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hsrm.TTC23
{
    internal class KmehrToFhir : ReflectiveSynchronization
    {
        public static string Patient { get; set; }

        public class MainRule : SynchronizationRule<kmehrmessageType, Bundle>
        {
            public override void DeclareSynchronization()
            {
                Synchronize(SyncRule<SynchronizeTransaction>(),
                    kmehr => kmehr.ForceFolder().ForceTransaction(),
                    bundle => Self(bundle));
            }

            protected override Bundle CreateRightOutput(kmehrmessageType input, IEnumerable<Bundle> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return new Bundle
                {
                    type = new BundleType { value = BundleTypeEnum.document }
                };
            }

            [LensPut(typeof(MainRule), nameof(SetBundle))]
            public static Bundle Self(Bundle that)
            {
                return that;
            }

            public static void SetBundle(Bundle self, Bundle that)
            {
                self.entry = that.entry;
            }
        }

        public class SynchronizeTransaction : SynchronizationRule<transactionType, Bundle>
        {
            public override void DeclareSynchronization()
            {
                SynchronizeMany(SyncRule<TransformProblem>(),
                    t => t.item.Where(it => it.HasCd("problem")),
                    b => (new ResourceCollection(b)).OfType<Resource, Condition>());

                SynchronizeMany(SyncRule<TransformMedication>(),
                    t => t.item.Where(it => it.HasCd("medication")),
                    b => (new ResourceCollection(b)).OfType<Resource, Medication>());

                SynchronizeMany(SyncRule<TransformAllergy>(),
                    t => t.item.Where(it => it.HasCd("allergy")),
                    b => (new ResourceCollection(b)).OfType<Resource, AllergyIntolerance>());

                //SynchronizeMany(SyncRule<TransformAdr>(),
                //    t => t.item.Where(it => it.HasCd("adr")),
                //    b => (new ResourceCollection(b)).OfType<Resource, AllergyIntolerance>());
            }
        }

        public abstract class CommonRule<TFhir> : SynchronizationRule<itemType, TFhir> where TFhir : Resource
        {
            protected abstract string KmehrType { get; }

            private int counter;

            protected itemType ApplyCd(itemType item)
            {
                item.id = new[]
                {
                    new IDKMEHR
                    {
                        S = IDKMEHRschemes.IDKMEHR,
                        SV = "1.0",
                        Value = (counter++).ToString()
                    }
                };
                item.cd = new CDITEM[]
                {
                    new CDITEM
                    {
                        S = CDITEMschemes.CDITEM,
                        SV = "1.11",
                        Value = KmehrType
                    }
                };
                return item;
            }

            protected TFhir AddId(TFhir resource)
            {
                resource.id = new id
                {
                    value = Guid.NewGuid().ToString()
                };
                return resource;
            }
        }

        public class TransformPatient : SynchronizationRule<personType, Patient>
        {
            public override void DeclareSynchronization()
            {
            }
        }

        public class TransformGmdManager : SynchronizationRule<itemType, Practitioner>
        {
            public override void DeclareSynchronization()
            {
            }
        }

        private static readonly Dictionary<CDCONTENTschemes, string> ClinicalCodes = new()
        {
            [CDCONTENTschemes.ICPC] = "http://hl7.org/fhir/sid/icpc-2",
            [CDCONTENTschemes.ICD] = "http://hl7.org/fhir/sid/icd-10"
        };

        private static readonly Dictionary<string, CDCONTENTschemes> ClinicalCodesReverted = ClinicalCodes.ToDictionary(pair => pair.Value, pair => pair.Key);

        public class TransformProblem : CommonRule<Condition>
        {
            protected override string KmehrType => "problem";

            public override void DeclareSynchronization()
            {
                Synchronize(it => it.beginmoment.FindBeginMoment().ToInvariantString(),
                            cnd => ((dateTime)cnd.Item).value);
                Synchronize(it => EnumConverter<CDLIFECYCLEvalues>.ToString(it.lifecycle.cd.Value),
                    cnd => cnd.clinicalStatus.coding[0].code.value);
                Synchronize(it => EnumConverter<CDLIFECYCLEvalues>.ToString(it.lifecycle.cd.Value),
                    cnd => cnd.clinicalStatus.coding[0].display.value);
                SynchronizeMany(SyncRule<ProblemCdToCode>(),
                    it => it.content[0].Items.OfType<object, CDCONTENT>().Where(cd => ClinicalCodes.ContainsKey(cd.S)),
                    cnd => cnd.code.coding);
            }

            protected override itemType CreateLeftOutput(Condition input, IEnumerable<itemType> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return ApplyCd(new itemType
                {

                });
            }

            protected override Condition CreateRightOutput(itemType input, IEnumerable<Condition> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return AddId( new Condition
                {
                    clinicalStatus = new CodeableConcept
                    {
                        coding = { new Coding
                        {
                            system = new uri { value = "http://terminology.hl7.org/CodeSystem/condition-ver-status" },
                            code = new code(),
                            display = new @string()
                        } }
                    },
                    verificationStatus = new CodeableConcept
                    {
                        coding = { new Coding
                        {
                            system = new uri { value = "http://terminology.hl7.org/CodeSystem/condition-ver-status" },
                            code = new code { value = "confirmed" },
                            display = new @string { value = "Confirmed" }
                        } }
                    },
                    category = { new CodeableConcept
                    {
                        coding = { new Coding
                        {
                            system = new uri { value = "http://loinc.org" },
                            code = new code { value = "75326-9" },
                            display = new @string { value = "Problem" }
                        } }
                    } },
                    subject = new Reference
                    {
                        reference = new @string
                        {
                            value = Patient
                        }
                    },
                    code = new CodeableConcept(),
                    Item = new dateTime
                    {
                        value = input.beginmoment.Items.OfType<DateTime>().ToString()
                    }
                });
            }
        }

        public class ProblemCdToCode : SynchronizationRule<CDCONTENT, Coding>
        {
            public override void DeclareSynchronization()
            {
                Synchronize(cd => GetFhirCode(cd.S), coding => coding.system.value);
                Synchronize(cd => cd.DN, coding => coding.display.value);
                Synchronize(cd => cd.Value, coding => coding.code.value);
            }

            protected override Coding CreateRightOutput(CDCONTENT input, IEnumerable<Coding> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return new Coding
                {
                    system = new uri(),
                    display = new @string(),
                    code = new code()
                };
            }

            [LensPut(typeof(ProblemCdToCode), nameof(PutFhirCode))]
            public static string GetFhirCode(CDCONTENTschemes scheme) => ClinicalCodes[scheme];

            public static CDCONTENTschemes PutFhirCode(CDCONTENTschemes current, string code)
                => ClinicalCodesReverted.TryGetValue(code, out var newFhirCode) ? newFhirCode : current;
        }

        public class TransformMedication : CommonRule<Medication>
        {
            protected override string KmehrType => "medication";

            protected override Medication CreateRightOutput(itemType input, IEnumerable<Medication> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return AddId( new Medication
                {
                    status = new MedicationStatusCodes(),
                    code = new CodeableConcept()
                });
            }

            protected override itemType CreateLeftOutput(Medication input, IEnumerable<itemType> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return ApplyCd( new itemType
                {
                    content =
                    {
                        new contentType(),
                        new contentType
                        {
                            Items =
                            {
                                new textType
                                {
                                    L = "fr",
                                }
                            }
                        }
                    }
                });
            }

            public override void DeclareSynchronization()
            {
                SynchronizeMany(SyncRule<MedicinalProductToCodedConcept>(),
                    it => it.content[0].Items.OfType<object, medicinalProductType>(),
                    med => med.code.coding);
                //SynchronizeLate(it => ((textType)it.content[1].Items[0]).Value,
                //    med => med.code.coding[0].display.value);
            }
        }

        public class TransformVaccine : CommonRule<Immunization>
        {
            protected override string KmehrType => "vaccine";

            protected override itemType CreateLeftOutput(Immunization input, IEnumerable<itemType> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return ApplyCd( new itemType
                {
                    content =
                    {
                        new contentType(),
                        new contentType
                        {
                            Items =
                            {
                                new CDCONTENT
                                {
                                    SV = "1.0",
                                    S = CDCONTENTschemes.CDVACCINEINDICATION,
                                }
                            }
                        }
                    },
                    beginmoment = new momentType
                    {
                        Items = { new date() }
                    }
                });
            }

            protected override Immunization CreateRightOutput(itemType input, IEnumerable<Immunization> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return AddId( new Immunization
                {
                    status = new ImmunizationStatusCodes(),
                    vaccineCode = new CodeableConcept
                    {
                        coding =
                        {
                            new Coding
                            {
                                system = new uri { value = "https://www.ehealth.fgov.be/standards/kmehr/en/tables/vaccine-indication-codes" },
                                code = new code()
                            }
                        }
                    },
                    administeredProduct = new CodeableReference
                    {
                        concept = new CodeableConcept()
                    },
                    Item = new dateTime()
                });
            }

            public override void DeclareSynchronization()
            {
                SynchronizeMany(SyncRule<MedicinalProductToCodedConcept>(),
                    it => it.content[0].Items.OfType<object, medicinalProductType>(),
                    imm => imm.administeredProduct.concept.coding);
                Synchronize(
                    it => ((CDCONTENT)it.content[1].Items[0]).Value,
                    imm => imm.vaccineCode.coding[0].code.value);
                Synchronize(
                    it => it.beginmoment.FindBeginMoment().ToInvariantString(),
                    imm => ((dateTime)imm.Item).value);
            }
        }

        public class TransformAdr : CommonRule<AllergyIntolerance>
        {
            protected override string KmehrType => "adr";

            public override void DeclareSynchronization()
            {
            }
        }

        public class TransformAllergy : CommonRule<AllergyIntolerance>
        {
            protected override string KmehrType => "allergy";

            public override void DeclareSynchronization()
            {
            }

            protected override itemType CreateLeftOutput(AllergyIntolerance input, IEnumerable<itemType> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return ApplyCd( new itemType
                {
                    content =
                    {
                        new contentType(),
                        new contentType
                        {
                            Items =
                            {
                                new textType { L = "en"}
                            }
                        }
                    }
                });
            }

            protected override AllergyIntolerance CreateRightOutput(itemType input, IEnumerable<AllergyIntolerance> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return AddId( new AllergyIntolerance
                {
                    clinicalStatus = new CodeableConcept
                    {
                        coding =
                        {
                            new Coding
                            {
                                system = new uri { value = "http://terminology.hl7.org/CodeSystem/allergyintolerance-clinical"},
                                code = new code{ value = "active"},
                                display = new @string {value  = "active"},
                            }
                        }
                    },
                    verificationStatus = new CodeableConcept
                    {
                        coding =
                        {
                            new Coding
                            {
                                system = new uri { value= "http://terminology.hl7.org/CodeSystem/allergyintolerance-verification"},
                                code = new code{ value = "confirmed"},
                                display = new @string { value = "Confirmed"},
                            }
                        }
                    }
                });
            }
        }

        public class MedicinalProductToCodedConcept : SynchronizationRule<medicinalProductType, Coding>
        {
            protected override medicinalProductType CreateLeftOutput(Coding input, IEnumerable<medicinalProductType> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return new medicinalProductType
                {
                    intendedcd = new CDDRUGCNK[]
                    {
                        new CDDRUGCNK { SV = "1999", S = CDDRUGCNKschemes.CDDRUGCNK }
                    }
                };
            }

            protected override Coding CreateRightOutput(medicinalProductType input, IEnumerable<Coding> candidates, ISynchronizationContext context, out bool existing)
            {
                existing = false;
                return new Coding()
                {
                    system = new uri { value = "https://www.ehealth.fgov.be/standards/fhir/medication/NamingSystem/cnk-codes" },
                    code = new code(),
                    display = new @string()
                };
            }

            public override void DeclareSynchronization()
            {
                Synchronize(med => med.intendedcd[0].Value, code => code.code.value);
                Synchronize(med => med.intendedname, code => code.display.value);
            }
        }
    }
}
