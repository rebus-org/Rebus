using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Rebus.Transports.Msmq
{
    public class WindowsPlatform
    {
        const int NO_ERROR = 0;
        const int ERROR_INSUFFICIENT_BUFFER = 122;

        enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool LookupAccountSid(string lpSystemName,
                                            [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
                                            StringBuilder lpName,
                                            ref uint cchName,
                                            StringBuilder ReferencedDomainName,
                                            ref uint cchReferencedDomainName,
                                            out SID_NAME_USE peUse);


        /// <summary>
        /// Looks up the name of the administrators group.
        /// </summary>
        /// <exception cref="ApplicationException">Gets thrown if something goes wrong during the lookup</exception>
        public string GetAdministratorAccountName()
        {
            // Sid for BUILTIN\Administrators
            var sid = new byte[] { 1, 2, 0, 0, 0, 0, 0, 5, 32, 0, 0, 0, 32, 2 };

            // MHG: I think the byte array above corresponds to "S-1-5-32-544" from this list:
            // http://support.microsoft.com/kb/243330 - just wondering why bytes were used in the example I found??

            return LookupSid(sid);
        }

        static string LookupSid(byte[] sid)
        {
            var name = new StringBuilder();
            var cchName = (uint) name.Capacity;
            var referencedDomainName = new StringBuilder();
            var cchReferencedDomainName = (uint) referencedDomainName.Capacity;
            SID_NAME_USE sidUse;
            var err = NO_ERROR;

            if (!LookupAccountSid(null, sid, name, ref cchName, referencedDomainName, ref cchReferencedDomainName,
                                  out sidUse))
            {
                err = Marshal.GetLastWin32Error();

                if (err == ERROR_INSUFFICIENT_BUFFER)
                {
                    name.EnsureCapacity((int) cchName);
                    referencedDomainName.EnsureCapacity((int) cchReferencedDomainName);
                    err = NO_ERROR;

                    if (!LookupAccountSid(null, sid, name, ref cchName, referencedDomainName,
                                          ref cchReferencedDomainName, out sidUse))
                    {
                        err = Marshal.GetLastWin32Error();
                    }
                }
            }

            if (err == 0)
            {
                return name.ToString();
            }

            throw new ApplicationException("Could not determine name of administrators group!");
        }
    }
}