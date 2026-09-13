
interface PendingApprovalProps {
  email: string;
  onBackToLogin: () => void;
}

export default function PendingApproval({ email, onBackToLogin }: PendingApprovalProps) {
  return (
    <div className="auth-card-container">
      <div className="auth-card glassmorphism pending-card">
        <div className="auth-header">
          <div className="brand-logo pending-logo-pulse">
            <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 22C17.5228 22 22 17.5228 22 12C22 6.47715 17.5228 2 12 2C6.47715 2 2 6.47715 2 12C2 17.5228 6.47715 22 12 22Z" stroke="var(--color-lobster-pink)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
              <path d="M12 6V12L16 14" stroke="var(--color-lobster-pink)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </div>
          <h2>Pending Approval</h2>
          <p className="subtitle">Your request has been received and is waiting for review.</p>
        </div>

        <div className="pending-body text-center">
          <div className="email-badge">
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"></path>
              <polyline points="22,6 12,13 2,6"></polyline>
            </svg>
            <span>{email}</span>
          </div>

          <div className="pending-message">
            <p>
              By default, all new accounts are created as <strong>pending approval</strong> for security reasons. 
              An administrator must manually approve your request before you can access the system.
            </p>
            <p className="highlight-text">
              Status: <strong>Under Admin Review</strong> (is_approved = false)
            </p>
          </div>

          <div className="instructions">
            <h4>What happens next?</h4>
            <ol>
              <li>The system administrator is notified of your registration request.</li>
              <li>Once verified, the admin will enable your account.</li>
              <li>You will then be able to log in using your password.</li>
            </ol>
          </div>
        </div>

        <button type="button" onClick={onBackToLogin} className="btn btn-outline">
          Back to Sign In
        </button>
      </div>
    </div>
  );
}
