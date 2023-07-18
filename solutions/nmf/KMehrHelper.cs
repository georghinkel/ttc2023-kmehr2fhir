using Hsrm.TTC23.Kmehr;
using NMF.Expressions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hsrm.TTC23.KmehrToFhir;

namespace nmf
{
    internal static class KMehrHelper
    {
        public static folderType ForceFolder(this kmehrmessageType kmehr)
        {
            var folder = kmehr.Items?.OfType<folderType>().FirstOrDefault();
            if (folder == null)
            {
                folder = new folderType();
                kmehr.Items = kmehr.Items.Add(folder);
            }
            return folder;
        }

        [LensPut(typeof(KMehrHelper), nameof(SetTransaction))]
        public static transactionType ForceTransaction(this folderType folder)
        {
            var transaction = folder.transaction?.FirstOrDefault();
            if ( transaction == null)
            {
                transaction = new transactionType();
                folder.transaction = new[] { transaction }; 
            }
            return transaction;
        }

        public static void SetTransaction(folderType folder, transactionType transaction)
        {
            if (folder.transaction == null)
            {
                folder.transaction = new[] { transaction };
            }
            else
            {
                folder.transaction[0] = transaction;
            }
        }



        [LensPut(typeof(KMehrHelper), nameof(SetBeginMoment))]
        public static DateTime FindBeginMoment(this momentType moment)
        {
            return moment.Items.OfType<DateTime>().FirstOrDefault();
        }

        public static void SetBeginMoment(momentType moment, DateTime beginMoment)
        {
            moment.Items = new object[] { beginMoment };
        }


        [LensPut(typeof(KMehrHelper), nameof(Parse))]
        public static string ToInvariantString(this DateTime time)
        {
            return time.ToString("s");
        }

        public static DateTime Parse(DateTime current, string time)
        {
            return DateTime.TryParse(time, CultureInfo.InvariantCulture, out var newTime) ? newTime : current;
        }

        public static bool HasCd(this itemType item, string cd)
        {
            return item.cd[0].Value == cd;
        }
    }
}
