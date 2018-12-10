/*
* FILE          : Billing.cs - Definitions
* PROJECT       : INFO-2180 Software Quality 1, Term Project
* PROGRAMMER    : Austin Zalac
* FIRST VERSION : November 16, 2018
*/
using System;

namespace Definitions
{
	/// <summary>
	/// All valid billing code responses.
	/// </summary>
	public enum BillingCodeResponse
	{
		NONE,
		PAID,
		DECL,
		FHCV,
		CMOH
	}
}

