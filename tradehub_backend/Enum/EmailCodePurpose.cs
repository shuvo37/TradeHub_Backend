namespace TradeHub.Enums;

// What a confirmation code sent by email is for. One active code per user and purpose.
// Stored as an integer: new values go at the END (the next one goes after PasswordReset).
public enum EmailCodePurpose
{
    ContactEmailVerification,  // confirming the contact email of the account
    PasswordReset              // "forgot password": proves the person owns the account's confirmed email
}
