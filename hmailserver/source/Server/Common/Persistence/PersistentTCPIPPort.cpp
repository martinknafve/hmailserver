// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "StdAfx.h"

#include ".\PersistentTCPIPPort.h"
#include "..\BO\TCPIPPort.h"
#include "..\SQL\SQLStatement.h"
#include "../SQL/IPAddressSQLHelper.h"

#ifdef _DEBUG
#define DEBUG_NEW new(_NORMAL_BLOCK, __FILE__, __LINE__)
#define new DEBUG_NEW
#endif

namespace HM
{
   PersistentTCPIPPort::PersistentTCPIPPort(void)
   {
   }

   PersistentTCPIPPort::~PersistentTCPIPPort(void)
   {
   }

   bool
   PersistentTCPIPPort::DeleteObject(shared_ptr<TCPIPPort> pObject)
   {
      SQLCommand command("delete from hm_tcpipports where portid = @PORTID");
      command.AddParameter("@PORTID", pObject->GetID());

      return Application::Instance()->GetDBManager()->Execute(command);
   }

   bool 
   PersistentTCPIPPort::ReadObject(shared_ptr<TCPIPPort> pObject, shared_ptr<DALRecordset> pRS)
   {
      IPAddressSQLHelper helper;

      pObject->SetID (pRS->GetLongValue("portid"));
      pObject->SetProtocol((SessionType) pRS->GetLongValue("portprotocol"));
      pObject->SetPortNumber(pRS->GetLongValue("portnumber"));
      pObject->SetUseSSL(pRS->GetLongValue("portusessl") ? true : false);
      pObject->SetAddress(helper.Construct(pRS, "portaddress1", "portaddress2"));
      pObject->SetSSLCertificateID(pRS->GetLongValue("portsslcertificateid"));
      
      return true;
   }

   bool 
   PersistentTCPIPPort::SaveObject(shared_ptr<TCPIPPort> pObject, String &errorMessage)
   {
      // errorMessage - not supported yet.

      return SaveObject(pObject);
   }

   bool 
   PersistentTCPIPPort::SaveObject(shared_ptr<TCPIPPort> pObject)
   {
      SQLStatement oStatement;
      oStatement.SetTable("hm_tcpipports");
      
      if (pObject->GetID() == 0)
      {
         oStatement.SetStatementType(SQLStatement::STInsert);
         oStatement.SetIdentityColumn("portid");
      }
      else
      {
         oStatement.SetStatementType(SQLStatement::STUpdate);
         String sWhere;
         sWhere.Format(_T("portid = %I64d"), pObject->GetID());
         oStatement.SetWhereClause(sWhere);
         
      }

      __int64 iAddress = 0;

      IPAddressSQLHelper helper;
      

      oStatement.AddColumn("portprotocol", pObject->GetProtocol());
      oStatement.AddColumn("portnumber", pObject->GetPortNumber());
      oStatement.AddColumnInt64("portsslcertificateid", pObject->GetSSLCertificateID());
      helper.AppendStatement(oStatement, pObject->GetAddress(), "portaddress1", "portaddress2");
      oStatement.AddColumn("portusessl", pObject->GetUseSSL());
      
      bool bNewObject = pObject->GetID() == 0;

      // Save and fetch ID
      __int64 iDBID = 0;
      bool bRetVal = Application::Instance()->GetDBManager()->Execute(oStatement, bNewObject ? &iDBID : 0);
      if (bRetVal && bNewObject)
         pObject->SetID((int) iDBID);


      return true;
   }
}