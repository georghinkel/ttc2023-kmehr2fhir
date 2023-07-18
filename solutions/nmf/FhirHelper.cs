using Hsrm.TTC23.Fhir;
using NMF.Collections.ObjectModel;
using NMF.Expressions.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nmf
{
    internal class FhirHelper
    {
    }

    internal class ResourceCollection : CustomCollection<Resource> 
    {
        private Bundle _bundle;

        public ResourceCollection(Bundle bundle)
            : base(bundle.entry.Select(entry => entry.resource.Item))
        {
            _bundle = bundle;
        }

        public override void Add(Resource item)
        {
            var entry = new BundleEntry
            {
                fullUrl = new uri { value = "urn:uuid:" + item.id.value },
                resource = new ResourceContainer { Item = item }
            };
            _bundle.entry.Add(entry);
        }

        public override void Clear()
        {
            _bundle.entry.Clear();
        }

        public override bool Remove(Resource item)
        {
            var entry = _bundle.entry.AsEnumerable().FirstOrDefault(e => e.resource.Item == item);
            if (entry != null)
            {
                return _bundle.entry.Remove(entry);
            }
            return false;
        }
    }
}
