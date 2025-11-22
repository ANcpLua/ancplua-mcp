# Signed Commits Guide

This guide explains how to set up and use signed commits in this repository.

## Why Sign Commits?

Signed commits provide:
- **Authenticity**: Proves that commits actually come from you
- **Integrity**: Ensures commits haven't been tampered with
- **Trust**: Shows verified checkmarks on GitHub
- **Security**: Required by our branch protection rules

## Setup Guide

### Option 1: GPG Signing (Recommended)

#### 1. Install GPG

**macOS:**
```bash
brew install gnupg
```

**Linux:**
```bash
sudo apt-get install gnupg  # Debian/Ubuntu
sudo yum install gnupg      # RHEL/CentOS
```

**Windows:**
Download from https://www.gnupg.org/download/

#### 2. Generate GPG Key

```bash
gpg --full-generate-key
```

Choose:
- Kind: `(1) RSA and RSA`
- Key size: `4096`
- Expiration: `0` (never) or set an expiration date
- Name: Your real name (matching GitHub)
- Email: Your GitHub email

#### 3. Get Your GPG Key ID

```bash
gpg --list-secret-keys --keyid-format=long
```

Output will look like:
```
sec   rsa4096/3AA5C34371567BD2 2024-01-01 [SC]
```

The key ID is `3AA5C34371567BD2`.

#### 4. Export Public Key

```bash
gpg --armor --export 3AA5C34371567BD2
```

Copy the output (including `-----BEGIN PGP PUBLIC KEY BLOCK-----` and `-----END PGP PUBLIC KEY BLOCK-----`).

#### 5. Add Key to GitHub

1. Go to GitHub Settings → SSH and GPG keys
2. Click "New GPG key"
3. Paste your public key
4. Click "Add GPG key"

#### 6. Configure Git

```bash
# Set your GPG key
git config --global user.signingkey 3AA5C34371567BD2

# Enable commit signing by default
git config --global commit.gpgsign true

# Enable tag signing by default
git config --global tag.gpgsign true

# Set GPG program (if needed)
git config --global gpg.program gpg
```

#### 7. Configure GPG TTY (for terminal prompts)

Add to your `~/.bashrc`, `~/.zshrc`, or `~/.bash_profile`:

```bash
export GPG_TTY=$(tty)
```

Reload your shell:
```bash
source ~/.zshrc  # or ~/.bashrc
```

### Option 2: SSH Signing (Simpler Alternative)

#### 1. Generate SSH Key (if you don't have one)

```bash
ssh-keygen -t ed25519 -C "your.email@example.com"
```

#### 2. Add SSH Key to GitHub

1. Copy your public key:
   ```bash
   cat ~/.ssh/id_ed25519.pub
   ```

2. Go to GitHub Settings → SSH and GPG keys
3. Click "New SSH key"
4. Choose key type: "Signing Key"
5. Paste your public key
6. Click "Add SSH key"

#### 3. Configure Git for SSH Signing

```bash
# Enable SSH signing
git config --global gpg.format ssh

# Set your SSH key for signing
git config --global user.signingkey ~/.ssh/id_ed25519.pub

# Enable commit signing by default
git config --global commit.gpgsign true
```

## Usage

### Signing Individual Commits

If you haven't enabled automatic signing:

```bash
git commit -S -m "Your commit message"
```

### Signing Tags

```bash
git tag -s v1.0.0 -m "Version 1.0.0"
```

### Verifying Signatures

```bash
# Verify last commit
git verify-commit HEAD

# Show signature in log
git log --show-signature -1
```

## Troubleshooting

### GPG: "failed to sign the data"

**Solution 1:** Make sure GPG_TTY is set:
```bash
export GPG_TTY=$(tty)
```

**Solution 2:** Test GPG:
```bash
echo "test" | gpg --clearsign
```

**Solution 3:** Kill and restart GPG agent:
```bash
gpgconf --kill gpg-agent
gpgconf --launch gpg-agent
```

### "No secret key"

Your key might have expired or been deleted. Generate a new one:
```bash
gpg --full-generate-key
```

### GPG Password Prompt Not Appearing

Install pinentry:
```bash
# macOS
brew install pinentry-mac

# Linux
sudo apt-get install pinentry-gtk-2

# Then configure GPG to use it
echo "pinentry-program $(which pinentry-mac)" >> ~/.gnupg/gpg-agent.conf
gpgconf --kill gpg-agent
```

### Commits Not Showing "Verified" on GitHub

1. Check that your email matches GitHub:
   ```bash
   git config user.email
   ```

2. Check that your GPG key is added to GitHub

3. Make sure the key hasn't expired:
   ```bash
   gpg --list-keys
   ```

## Claude Code / AI Assistants

If you're using Claude Code or other AI assistants that commit on your behalf:

### Option 1: Configure Co-Authored-By

Your commits can include co-author information:

```bash
git commit -m "Your message

Co-Authored-By: Claude <noreply@anthropic.com>"
```

### Option 2: Sign AI-Generated Commits

Since you're reviewing and approving the changes, signing them is appropriate:

```bash
# Claude Code will use your configured signing key automatically
# if commit.gpgsign is true
```

## Branch Protection

This repository requires signed commits on protected branches (`main`, `develop`).

**What this means:**
- All commits to these branches must be signed
- PRs with unsigned commits will be blocked
- You must have signing configured before contributing

**Emergency Bypass:**
Repository administrators can bypass this requirement, but it's strongly discouraged.

## Quick Reference

```bash
# GPG Signing Setup
gpg --full-generate-key
gpg --list-secret-keys --keyid-format=long
gpg --armor --export YOUR_KEY_ID
git config --global user.signingkey YOUR_KEY_ID
git config --global commit.gpgsign true

# SSH Signing Setup
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true

# Verify Your Setup
git commit --allow-empty -m "Test signed commit"
git verify-commit HEAD
```

## Additional Resources

- [GitHub: Signing commits](https://docs.github.com/en/authentication/managing-commit-signature-verification/signing-commits)
- [GitHub: Generating a new GPG key](https://docs.github.com/en/authentication/managing-commit-signature-verification/generating-a-new-gpg-key)
- [GitHub: Telling Git about your signing key](https://docs.github.com/en/authentication/managing-commit-signature-verification/telling-git-about-your-signing-key)
- [GPG documentation](https://www.gnupg.org/documentation/)
