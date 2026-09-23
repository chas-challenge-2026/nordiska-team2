import argparse
import json
import os
from datetime import datetime, timedelta

def generate_mock_data(count: int, output_path: str):
    first_names = ["Anna", "Johan", "Maria", "Karl", "Elin", "Erik", "Sara", "Lars", "Karin", "Per"]
    last_names = ["Andersson", "Johansson", "Karlsson", "Nilsson", "Eriksson", "Larsson", "Olsson", "Persson"]
    streets = ["Storgatan", "Kungsgatan", "Drottninggatan", "Sveavägen", "Vasagatan"]
    
    # Ensure directory exists if output_path includes subdirectories
    output_dir = os.path.dirname(output_path)
    if output_dir:
        os.makedirs(output_dir, exist_ok=True)
    
    print(f"Generating {count:,} user profile(s) with 50 transactions each...")
    
    all_reports = []
    
    for i in range(1, count + 1):
        name = f"{first_names[i % len(first_names)]} {last_names[(i // 3) % len(last_names)]}"
        personal_id = f"198{i % 10}{(i * 7) % 10}0{i % 2 + 1}{(i * 3) % 28 + 1:02d}{i % 9000 + 1000:04d}"
        customer_id = str(1000 + i)
        
        balance = 50000.00
        transactions = []
        start_date = datetime(2026, 1, 1)
        
        descriptions = ["Lön", "Hyra", "Mat", "Transport", "Insättning", "Elräkning", "Nöje", "Fonder"]
        
        for t in range(1, 51):
            tx_date = start_date + timedelta(days=t * 7)
            is_deposit = (t % 3 == 0)
            amount = float((t * 123) % 4500 + 50)
            if not is_deposit:
                amount = -amount
                
            balance += amount
            
            transactions.append({
                "id": f"TX-{100000 + i * 100 + t}",
                "date": tx_date.strftime("%Y-%m-%d"),
                "type": "deposit" if is_deposit else "withdrawal",
                "description": descriptions[t % len(descriptions)],
                "amount_sek": round(amount, 2),
                "balance_after_sek": round(balance, 2)
            })
            
        report = {
            "metadata": {
                "report_id": f"TAX-2026-{i:05d}",
                "report_type": "ANNUAL_TAX_REPORT",
                "year": 2026,
                "period_start": "2026-01-01",
                "period_end": "2026-12-31",
                "generation_date": "2027-01-02T02:00:00Z"
            },
            "customer": {
                "customer_id": customer_id,
                "personal_id": personal_id,
                "full_name": name,
                "address": f"{streets[i % len(streets)]} {i % 100 + 1}, 111 22 Stockholm"
            },
            "account": {
                "account_number": f"9150-{i:09d}",
                "account_type": "Sparkonto Plus",
                "interest_rate_pct": 3.50
            },
            "summary": {
                "starting_balance_sek": 50000.00,
                "ending_balance_sek": round(balance, 2),
                "total_deposits_sek": 25000.00,
                "total_withdrawals_sek": 13000.00,
                "total_interest_earned_sek": 1850.00,
                "total_tax_withheld_sek": 1400.00
            },
            "transactions": transactions
        }
        all_reports.append(report)
        
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(all_reports, f, ensure_ascii=False, indent=2)
        
    file_size_mb = os.path.getsize(output_path) / (1024 * 1024)
    print(f"Successfully generated {output_path} ({file_size_mb:.2f} MB)")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate mock tax report JSON data.")
    parser.add_argument("count", type=int, help="Number of users/items to generate")
    parser.add_argument("path", type=str, help="File path where the JSON should be saved")
    
    args = parser.parse_args()
    generate_mock_data(args.count, args.path)
