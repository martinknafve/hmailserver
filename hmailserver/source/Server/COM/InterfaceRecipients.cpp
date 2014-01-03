// Copyright (c) 2010 Martin Knafve / hMailServer.com.  
// http://www.hmailserver.com

#include "stdafx.h"
#include "COMError.h"
#include "InterfaceRecipients.h"
#include "InterfaceRecipient.h"

#include "../Common/BO/Message.h"
#include "../Common/BO/MessageRecipient.h"
#include "../Common/BO/MessageRecipients.h"


void
InterfaceRecipients::Attach(shared_ptr<HM::Message> pMessage)
{
   m_pMessage = pMessage;

   
}

STDMETHODIMP 
InterfaceRecipients::get_Count(long *pVal)
{
   try
   {
      if (!m_pMessage)
         return GetAccessDenied();

      std::vector<shared_ptr<HM::MessageRecipient> > vecRecipients = m_pMessage->GetRecipients()->GetVector();
   
      *pVal = (int) vecRecipients.size();
      return S_OK;
   }
   catch (...)
   {
      return COMError::GenerateGenericMessage();
   }
}

STDMETHODIMP 
InterfaceRecipients::get_Item(long Index, IInterfaceRecipient **pVal)
{
   try
   {
      if (!m_pMessage)
         return GetAccessDenied();

      CComObject<InterfaceRecipient>* pInterfaceRecipient = new CComObject<InterfaceRecipient>();
      pInterfaceRecipient->SetAuthentication(m_pAuthentication);
   
      std::vector<shared_ptr<HM::MessageRecipient> > vecRecipients = m_pMessage->GetRecipients()->GetVector();
   
      if (Index >= (long) vecRecipients.size())
         return DISP_E_BADINDEX;
   
      shared_ptr<HM::MessageRecipient> pRecipient = vecRecipients[Index];
   
      pInterfaceRecipient->AttachItem(pRecipient);
      pInterfaceRecipient->AddRef();
      *pVal = pInterfaceRecipient;
   
      return S_OK;
   }
   catch (...)
   {
      return COMError::GenerateGenericMessage();
   }
}


