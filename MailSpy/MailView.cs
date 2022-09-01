using System;
using System.Collections.Generic;
using Framework.Entities;

namespace MailSpy
{
    public class MailView : Entity
    {
        public String Sender { get; set; }

        public String Recipient { get; set; }

        public String HiddenCopy { get; set; }

        public String Subject { get; set; }

        public String Body { get; set; }

        public DateTime? DateAdded { get; set; }

        public DateTime? ShipDate { get; set; }

        public Int32 Attachments { get; set; }

        public String MailState { get; set; }

        public IEnumerable<String> Recipients
        {
            get
            {
                return this.Recipient.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public Boolean HasRecipients { get { return !String.IsNullOrWhiteSpace(this.Recipient); } }

        public IEnumerable<String> HiddenCopies
        {
            get
            {
                return this.HiddenCopy.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public Boolean HasHiddenCopies { get { return !String.IsNullOrWhiteSpace(this.HiddenCopy); } }

        public IEnumerable<ValueOf<String>> AttachmentList { get; internal set; }
    }
}