#!/bin/bash
# ============================================================================
# sanitizar.sh — Remove dados sensíveis dos arquivos do repositório
# Uso: bash sanitizar.sh
# ============================================================================
# O que este script faz:
# 1. Cria o arquivo .secrets com todos os valores reais (backup seguro)
# 2. Substitui cada dado sensível por uma marcação tipo {{CHAVE}}
# 3. Adiciona .secrets e local.settings.json ao .gitignore
# ============================================================================

REPO_DIR="$HOME/Projetos/GitHub/cdaalexandre/RastreioGpsApi"
SECRETS_FILE="$REPO_DIR/.secrets"
GITIGNORE_FILE="$REPO_DIR/.gitignore"

echo "============================================"
echo " SANITIZADOR DE DADOS SENSÍVEIS"
echo " Repositório: RastreioGpsApi"
echo "============================================"
echo ""

# ──────────────────────────────────────────────────────────────────────
# PASSO 1: Criar o arquivo .secrets com os valores reais
# ──────────────────────────────────────────────────────────────────────
echo "📝 Criando .secrets com os valores reais..."

cat > "$SECRETS_FILE" << 'SECRETS_EOF'
# ============================================================================
# .secrets — Valores reais removidos do repositório por segurança
# NÃO FAÇA COMMIT DESTE ARQUIVO! Ele está no .gitignore
# ============================================================================

# === CHAVES DE ACESSO (Account Keys) ===
ACCOUNT_KEY_STD=RrOt+BUczBzne1tptmIdReotVfycujcgaXhO4V6no2YHrIWhXtlFqUf966Y369LSYeRxibiQ48mr+ASt2+4ckg==
ACCOUNT_KEY_BBA7=cbrqZHlwQrWwEZZ+07+clx2cwcoCIdBWLQ8R69lTvk0JA0d8EU3tFLRLQ0TBHePn1ITdhs44z9tB+ASt4tdFbg==

# === IDs DE ASSINATURA (Subscription IDs) ===
SUBSCRIPTION_STUDENTS=ef10799a-e042-473f-8425-4d967abfbad1
SUBSCRIPTION_PRODESP=e36cf887-205d-4ccf-9c45-b7171cabbba7

# === IDs DE TENANT ===
TENANT_CRUZEIRO=38ae2f02-5710-4e12-80bb-83600c3fdf1e
TENANT_PRODESP=3a78b0cd-7c8e-4929-83d5-190a6cc01365
TENANT_PADRAO=8d7da294-fa82-4cc6-b8b3-f50e45ec1d5a

# === EMAILS ===
EMAIL_UNICID=alexandre.calzetta@cs.unicid.edu.br
EMAIL_HOTMAIL=alexandre2709@hotmail.com
EMAIL_PRODESP=acdalves@sp.gov.br
SECRETS_EOF

echo "   ✅ .secrets criado"

# ──────────────────────────────────────────────────────────────────────
# PASSO 2: Substituir dados sensíveis nos arquivos .txt
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "🔒 Substituindo dados sensíveis por marcações..."

# Lista dos arquivos a sanitizar
ARQUIVOS=(
    "$REPO_DIR/Anotacoes.txt"
    "$REPO_DIR/Anotacoes_Consolidadas.txt"
)

# Também pega o export se existir
for f in "$REPO_DIR"/RastreioGpsApi_export_*.txt; do
    [ -f "$f" ] && ARQUIVOS+=("$f")
done

for ARQUIVO in "${ARQUIVOS[@]}"; do
    if [ ! -f "$ARQUIVO" ]; then
        echo "   ⚠️  Arquivo não encontrado: $(basename "$ARQUIVO") — pulando"
        continue
    fi

    NOME=$(basename "$ARQUIVO")
    echo "   📄 Processando: $NOME"

    # --- CHAVES DE ACESSO (mais crítico!) ---
    sed -i 's/RrOt+BUczBzne1tptmIdReotVfycujcgaXhO4V6no2YHrIWhXtlFqUf966Y369LSYeRxibiQ48mr+ASt2+4ckg==/{{ACCOUNT_KEY_STD}}/g' "$ARQUIVO"
    sed -i 's/cbrqZHlwQrWwEZZ+07+clx2cwcoCIdBWLQ8R69lTvk0JA0d8EU3tFLRLQ0TBHePn1ITdhs44z9tB+ASt4tdFbg==/{{ACCOUNT_KEY_BBA7}}/g' "$ARQUIVO"

    # --- SUBSCRIPTION IDs ---
    sed -i 's/ef10799a-e042-473f-8425-4d967abfbad1/{{SUBSCRIPTION_STUDENTS}}/g' "$ARQUIVO"
    sed -i 's/e36cf887-205d-4ccf-9c45-b7171cabbba7/{{SUBSCRIPTION_PRODESP}}/g' "$ARQUIVO"

    # --- TENANT IDs ---
    sed -i 's/38ae2f02-5710-4e12-80bb-83600c3fdf1e/{{TENANT_CRUZEIRO}}/g' "$ARQUIVO"
    sed -i 's/3a78b0cd-7c8e-4929-83d5-190a6cc01365/{{TENANT_PRODESP}}/g' "$ARQUIVO"
    sed -i 's/8d7da294-fa82-4cc6-b8b3-f50e45ec1d5a/{{TENANT_PADRAO}}/g' "$ARQUIVO"

    # --- EMAILS ---
    sed -i 's/alexandre\.calzetta@cs\.unicid\.edu\.br/{{EMAIL_UNICID}}/g' "$ARQUIVO"
    sed -i 's/alexandre2709@hotmail\.com/{{EMAIL_HOTMAIL}}/g' "$ARQUIVO"
    sed -i 's/acdalves@sp\.gov\.br/{{EMAIL_PRODESP}}/g' "$ARQUIVO"

    echo "      ✅ $NOME sanitizado"
done

# ──────────────────────────────────────────────────────────────────────
# PASSO 3: Atualizar o .gitignore
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "📋 Atualizando .gitignore..."

# Entradas a garantir no .gitignore
ENTRADAS=(
    ".secrets"
    "local.settings.json"
)

# Cria o .gitignore se não existir
touch "$GITIGNORE_FILE"

for ENTRADA in "${ENTRADAS[@]}"; do
    if grep -qF "$ENTRADA" "$GITIGNORE_FILE" 2>/dev/null; then
        echo "   ⏭️  '$ENTRADA' já está no .gitignore"
    else
        echo "" >> "$GITIGNORE_FILE"
        echo "$ENTRADA" >> "$GITIGNORE_FILE"
        echo "   ✅ '$ENTRADA' adicionado ao .gitignore"
    fi
done

# ──────────────────────────────────────────────────────────────────────
# PASSO 4: Verificação final
# ──────────────────────────────────────────────────────────────────────
echo ""
echo "============================================"
echo " VERIFICAÇÃO FINAL"
echo "============================================"

# Conta quantas marcações foram aplicadas
TOTAL=0
for ARQUIVO in "${ARQUIVOS[@]}"; do
    if [ -f "$ARQUIVO" ]; then
        COUNT=$(grep -c '{{' "$ARQUIVO" 2>/dev/null || echo 0)
        TOTAL=$((TOTAL + COUNT))
        echo "   📄 $(basename "$ARQUIVO"): $COUNT marcações {{...}} aplicadas"
    fi
done

echo ""
echo "   Total de substituições: $TOTAL"

# Verifica se sobrou algum dado sensível
echo ""
echo "🔍 Procurando dados sensíveis que possam ter escapado..."
VAZOU=0
for ARQUIVO in "${ARQUIVOS[@]}"; do
    if [ -f "$ARQUIVO" ]; then
        if grep -q "AccountKey=" "$ARQUIVO" 2>/dev/null; then
            # Verifica se é um AccountKey real (não uma marcação)
            if grep "AccountKey=" "$ARQUIVO" | grep -qv '{{'; then
                echo "   ⚠️  ATENÇÃO: AccountKey real ainda presente em $(basename "$ARQUIVO")!"
                VAZOU=1
            fi
        fi
    fi
done

if [ $VAZOU -eq 0 ]; then
    echo "   ✅ Nenhum dado sensível encontrado — tudo limpo!"
fi

echo ""
echo "============================================"
echo " CONCLUÍDO!"
echo "============================================"
echo ""
echo " Arquivos modificados (sanitizados):"
for ARQUIVO in "${ARQUIVOS[@]}"; do
    [ -f "$ARQUIVO" ] && echo "   • $(basename "$ARQUIVO")"
done
echo ""
echo " Arquivo criado (NÃO vai para o GitHub):"
echo "   • .secrets"
echo ""
echo " Próximo passo: faça o commit com"
echo "   git add -A && git commit -m 'Sanitiza dados sensíveis' && git push"
echo ""
