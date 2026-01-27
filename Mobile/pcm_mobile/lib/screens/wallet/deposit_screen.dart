import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import 'package:font_awesome_flutter/font_awesome_flutter.dart';
import '../../config/theme.dart';
import '../../providers/wallet_provider.dart';

class DepositScreen extends StatefulWidget {
  const DepositScreen({super.key});

  @override
  State<DepositScreen> createState() => _DepositScreenState();
}

class _DepositScreenState extends State<DepositScreen> {
  final _formKey = GlobalKey<FormState>();
  final _amountController = TextEditingController();
  final List<int> _quickAmounts = [50000, 100000, 200000, 500000, 1000000, 2000000];
  int? _selectedQuickAmount;
  String _paymentMethod = 'BankTransfer';

  final List<Map<String, dynamic>> _paymentMethods = [
    {
      'id': 'Cash',
      'label': 'Tiền mặt',
      'icon': Icons.money,
      'color': Colors.green,
    },
    {
      'id': 'BankTransfer',
      'label': 'Ngân hàng',
      'icon': Icons.account_balance,
      'color': Colors.blue,
    },
    {
      'id': 'USDT',
      'label': 'USDT',
      'icon': Icons.attach_money,
      'color': Colors.teal,
    },
    {
      'id': 'EWallet',
      'label': 'Ví điện tử',
      'icon': Icons.account_balance_wallet,
      'color': Colors.purple,
    },
  ];

  @override
  void dispose() {
    _amountController.dispose();
    super.dispose();
  }

  Future<void> _submitDeposit() async {
    if (_formKey.currentState?.validate() ?? false) {
      final amount = double.tryParse(_amountController.text.replaceAll(RegExp(r'[^0-9]'), '')) ?? 0;
      final provider = context.read<WalletProvider>();
      
      final transaction = await provider.deposit(amount, null, _paymentMethod);
      
      if (transaction != null && mounted) {
        // Show sucess dialog
        await showDialog(
          context: context,
          builder: (c) => AlertDialog(
            title: const Text('Nạp tiền thành công!'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.check_circle, color: Colors.green, size: 64),
                const SizedBox(height: 16),
                Text('Bạn đã nạp ${NumberFormat.currency(locale: 'vi_VN', symbol: 'đ').format(amount)}'),
                const SizedBox(height: 8),
                const Text('Tiền đã được cộng vào ví của bạn.'),
              ],
            ),
            actions: [
              ElevatedButton(
                onPressed: () { 
                  Navigator.pop(c);
                  context.go('/wallet');
                }, 
                child: const Text('Đóng'),
              )
            ],
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final currencyFormat = NumberFormat.currency(locale: 'vi_VN', symbol: 'đ');
    final provider = context.watch<WalletProvider>();

    return Scaffold(
      appBar: AppBar(title: const Text('Nạp tiền')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // 1. CHỌN SỐ TIỀN
              const Text('1. Chọn số tiền', style: AppTheme.subheadingStyle),
              const SizedBox(height: 12),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: _quickAmounts.map((amount) => ChoiceChip(
                  label: Text(NumberFormat.simpleCurrency(locale: 'vi_VN', decimalDigits: 0).format(amount)),
                  selected: _selectedQuickAmount == amount,
                  onSelected: (s) => setState(() {
                     _selectedQuickAmount = amount; 
                     _amountController.text = amount.toStringAsFixed(0);
                  }),
                )).toList(),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _amountController,
                keyboardType: TextInputType.number,
                decoration: const InputDecoration(
                  labelText: 'Nhập số tiền khác', 
                  suffixText: 'đ',
                  border: OutlineInputBorder(),
                  prefixIcon: Icon(Icons.attach_money),
                ),
                validator: (v) {
                   final val = double.tryParse(v?.replaceAll(RegExp(r'[^0-9]'), '') ?? '') ?? 0;
                   if (val < 10000) return 'Tối thiểu 10,000đ';
                   if (val > 100000000) return 'Tối đa 100,000,000đ';
                   return null;
                },
                onChanged: (val) {
                  if (_selectedQuickAmount != null && val != _selectedQuickAmount.toString()) {
                    setState(() => _selectedQuickAmount = null);
                  }
                },
              ),
              
              const SizedBox(height: 32),
              
              // 2. CHỌN PHƯƠNG THỨC THANH TOÁN
              const Text('2. Phương thức thanh toán', style: AppTheme.subheadingStyle),
              const SizedBox(height: 12),
              GridView.builder(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  childAspectRatio: 2.5,
                  crossAxisSpacing: 12,
                  mainAxisSpacing: 12,
                ),
                itemCount: _paymentMethods.length,
                itemBuilder: (context, index) {
                  final method = _paymentMethods[index];
                  final isSelected = _paymentMethod == method['id'];
                  return InkWell(
                    onTap: () => setState(() => _paymentMethod = method['id']),
                    borderRadius: BorderRadius.circular(12),
                    child: Container(
                      decoration: BoxDecoration(
                        color: isSelected ? method['color'].withValues(alpha: 0.1) : Colors.white,
                        border: Border.all(
                          color: isSelected ? method['color'] : Colors.grey.shade300, 
                          width: isSelected ? 2 : 1
                        ),
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(method['icon'], color: method['color']),
                          const SizedBox(width: 8),
                          Text(method['label'], style: TextStyle(
                            fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                            color: isSelected ? method['color'] : Colors.black87
                          )),
                        ],
                      ),
                    ),
                  );
                },
              ),

              const SizedBox(height: 32),
              
              if (provider.error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 16),
                  child: Text(provider.error!, style: const TextStyle(color: Colors.red)),
                ),

              SizedBox(
                width: double.infinity,
                height: 50,
                child: ElevatedButton(
                  onPressed: provider.isLoading ? null : _submitDeposit,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppTheme.primaryColor,
                    foregroundColor: Colors.white,
                  ),
                  child: provider.isLoading 
                    ? const CircularProgressIndicator(color: Colors.white)
                    : const Text('GỬI YÊU CẦU NẠP TIỀN', style: TextStyle(fontWeight: FontWeight.bold)),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
