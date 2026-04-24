#!/bin/bash
# =============================================================================
# Remove Mock Authentication
# =============================================================================
# This script permanently removes mock authentication from the application,
# leaving only Azure Static Web Apps (SWA) authentication.
#
# After running this script:
# - The app will use Azure AD authentication via SWA
# - Mock login endpoints will return 404
# - The login.html page will no longer exist
# - Copilot instructions will be updated for SWA auth
#
# To restore mock auth, use: git checkout HEAD -- <files>
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo "=============================================="
echo "  Remove Mock Authentication"
echo "=============================================="
echo ""
echo "This will permanently remove mock authentication."
echo "The app will use Azure Static Web Apps (SWA) authentication instead."
echo ""
echo "Project root: $PROJECT_ROOT"
echo ""

# Confirm before proceeding
read -p "Are you sure you want to proceed? (y/N) " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 1
fi

echo ""
echo "Removing mock authentication files..."

# Backend files - Mock auth specific
rm -f "$PROJECT_ROOT/api/Auth/MockAuthProvider.cs" && echo "  ✓ Removed api/Auth/MockAuthProvider.cs"
rm -f "$PROJECT_ROOT/api/Auth/MockAuthRegistration.cs" && echo "  ✓ Removed api/Auth/MockAuthRegistration.cs"
rm -f "$PROJECT_ROOT/api/Auth/AuthRegistration.cs" && echo "  ✓ Removed api/Auth/AuthRegistration.cs"
rm -f "$PROJECT_ROOT/api/Functions/MockAuthFunctions.cs" && echo "  ✓ Removed api/Functions/MockAuthFunctions.cs"
rm -f "$PROJECT_ROOT/api/Functions/DemoAdminFunctions.cs" && echo "  ✓ Removed api/Functions/DemoAdminFunctions.cs"
rm -f "$PROJECT_ROOT/api/Services/SampleDataSeeder.cs" && echo "  ✓ Removed api/Services/SampleDataSeeder.cs"
rm -f "$PROJECT_ROOT/api/Models/AuthSession.cs" && echo "  ✓ Removed api/Models/AuthSession.cs"
rm -f "$PROJECT_ROOT/api/Models/User.cs" && echo "  ✓ Removed api/Models/User.cs"

# Backend files - Auth abstraction (not needed after mock removal)
rm -f "$PROJECT_ROOT/api/Auth/IAuthProvider.cs" && echo "  ✓ Removed api/Auth/IAuthProvider.cs"
rm -f "$PROJECT_ROOT/api/Auth/SwaAuthProvider.cs" && echo "  ✓ Removed api/Auth/SwaAuthProvider.cs"
rm -f "$PROJECT_ROOT/api/Functions/AuthFunctions.cs" && echo "  ✓ Removed api/Functions/AuthFunctions.cs"
rm -f "$PROJECT_ROOT/api/Models/AuthDtos.cs" && echo "  ✓ Removed api/Models/AuthDtos.cs"

# Clean up empty directories
rmdir "$PROJECT_ROOT/api/Auth" 2>/dev/null && echo "  ✓ Removed empty api/Auth/ directory" || true
rmdir "$PROJECT_ROOT/api/Services" 2>/dev/null && echo "  ✓ Removed empty api/Services/ directory" || true

# Frontend files
rm -f "$PROJECT_ROOT/login.html" && echo "  ✓ Removed login.html"
rm -f "$PROJECT_ROOT/js/authService.js" && echo "  ✓ Removed js/authService.js"

# Config files - delete mock template and active config, rename swa template to active
rm -f "$PROJECT_ROOT/staticwebapp.config.mock.json" && echo "  ✓ Removed staticwebapp.config.mock.json"
rm -f "$PROJECT_ROOT/staticwebapp.config.json" && echo "  ✓ Removed staticwebapp.config.json (active)"
if [ -f "$PROJECT_ROOT/staticwebapp.config.swa.json" ]; then
    mv "$PROJECT_ROOT/staticwebapp.config.swa.json" "$PROJECT_ROOT/staticwebapp.config.json"
    echo "  ✓ Renamed staticwebapp.config.swa.json → staticwebapp.config.json"
fi

# Data files
if [ -d "$PROJECT_ROOT/data" ]; then
    rm -rf "$PROJECT_ROOT/data" && echo "  ✓ Removed data/ directory"
fi

# Documentation
rm -f "$PROJECT_ROOT/docs/mock-authentication.md" && echo "  ✓ Removed docs/mock-authentication.md"
rm -f "$PROJECT_ROOT/docs/removing-mock-authentication.md" && echo "  ✓ Removed docs/removing-mock-authentication.md"

echo ""
echo "Updating Copilot instruction files..."

# Function to replace auth section in a file
replace_auth_section() {
    local target_file="$1"
    local replacement_file="$2"
    local file_name=$(basename "$target_file")
    
    if [ ! -f "$target_file" ]; then
        echo "  ⚠ Skipped $file_name (not found)"
        return
    fi
    
    if [ ! -f "$replacement_file" ]; then
        echo "  ⚠ Skipped $file_name (replacement template not found)"
        return
    fi
    
    # Check if the file has the auth section markers
    if ! grep -q "<!-- AUTH-SECTION-START -->" "$target_file"; then
        echo "  ⚠ Skipped $file_name (no AUTH-SECTION markers found)"
        return
    fi
    
    # Create a temporary file
    local temp_file=$(mktemp)
    
    # Extract content before AUTH-SECTION-START (including the heading line before it)
    sed -n '1,/<!-- AUTH-SECTION-START -->/p' "$target_file" | sed '$d' > "$temp_file"
    
    # Find and remove the heading line (# 🔐 ...) that precedes AUTH-SECTION-START
    # We need to remove it because the replacement file includes its own heading
    local last_heading_line=$(grep -n "^# 🔐" "$temp_file" | tail -1 | cut -d: -f1)
    if [ -n "$last_heading_line" ]; then
        head -n $((last_heading_line - 1)) "$temp_file" > "${temp_file}.tmp"
        mv "${temp_file}.tmp" "$temp_file"
    fi
    
    # Append the replacement content
    cat "$replacement_file" >> "$temp_file"
    
    # Append content after AUTH-SECTION-END
    sed -n '/<!-- AUTH-SECTION-END -->/,$p' "$target_file" | sed '1d' >> "$temp_file"
    
    # Replace the original file
    mv "$temp_file" "$target_file"
    echo "  ✓ Updated $file_name with SWA authentication instructions"
}

# Update copilot-instructions.md
replace_auth_section \
    "$PROJECT_ROOT/.github/copilot-instructions.md" \
    "$PROJECT_ROOT/.github/copilot-instructions.auth-swa.md"

# Update copilot-agent-instructions.md
replace_auth_section \
    "$PROJECT_ROOT/.github/copilot-agent-instructions.md" \
    "$PROJECT_ROOT/.github/copilot-agent-instructions.auth-swa.md"

# Remove the SWA auth template files (they're now embedded)
rm -f "$PROJECT_ROOT/.github/copilot-instructions.auth-swa.md" && echo "  ✓ Removed .github/copilot-instructions.auth-swa.md (template)"
rm -f "$PROJECT_ROOT/.github/copilot-agent-instructions.auth-swa.md" && echo "  ✓ Removed .github/copilot-agent-instructions.auth-swa.md (template)"

echo ""
echo "=============================================="
echo "  Mock authentication removed successfully!"
echo "=============================================="
echo ""
echo "Next steps:"
echo "  1. Rebuild the API: cd api && dotnet build"
echo "  2. Restart SWA: Run 'swa stop' then 'swa start' tasks"
echo "  3. Test: Navigate to http://localhost:4280"
echo ""
echo "The app now uses Azure Static Web Apps authentication."
echo "Users will be redirected to /.auth/login/aad for login."
echo ""
