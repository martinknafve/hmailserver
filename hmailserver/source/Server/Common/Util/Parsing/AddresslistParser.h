// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#pragma once

namespace HM
{
   class Address
   { 
   public:
      String sPersonalName;
      String sMailboxName;
      String sDomainName;
      
   };
   class AddresslistParser  
   {
   public:
	   AddresslistParser();
	   virtual ~AddresslistParser();
      
  
      std::vector<shared_ptr<Address> > ParseList(const String &sList) const;

      void ExtractParts(const String &sCompound, String &sFullName, String &sMailbox, String &sDomain) const;

   private:

      bool IsSeperator(char c) const;
      void UnescapeString(String &TheString) const;
      
      void _ExtractQuotedString(const String &sData, String &sQuotedString) const;
      void _ExtractWithinGTLT(const String &sData, String &sOutputString) const;
   };

   class AddresslistParserTester
   {
   public:
      AddresslistParserTester();

      bool Test();

   private:

      void _TestAddress(const String &sHeader, const String &sFullname, const String &sUser, const String &sDomain);
   };

}
