from pythonosc import osc_bundle_builder
from pythonosc import osc_message_builder
from pythonosc import udp_client
import time

client = udp_client.SimpleUDPClient("127.0.0.1", 9000)


def send_clk_and_data(clk=True, data=True):
    bundle = osc_bundle_builder.OscBundleBuilder(osc_bundle_builder.IMMEDIATELY)
    clk_msg = osc_message_builder.OscMessageBuilder(address="/avatar/parameters/SERIAL_CLK")
    clk_msg.add_arg(clk)
    data_msg = osc_message_builder.OscMessageBuilder(address="/avatar/parameters/MyParameter_DATA")
    data_msg.add_arg(data)
    bundle.add_content(clk_msg.build())
    bundle.add_content(data_msg.build())
    client.send(bundle.build())


number_of_bits = 12
data_to_transmit = 6 * 60 + 45  # The time is 6:45
seconds_between_msg = 10 / 4

send_clk_and_data(True, True)  # Idle
time.sleep(seconds_between_msg)
send_clk_and_data(False, False)  # Sync bit
time.sleep(seconds_between_msg)

for i in range(number_of_bits):
    send_clk_and_data(i % 2 == 0, (data_to_transmit >> i & 1) == 1)
    time.sleep(seconds_between_msg)

end_bit_clock = number_of_bits % 2 == 0
send_clk_and_data(end_bit_clock, True)

if not end_bit_clock:
    time.sleep(seconds_between_msg)
    send_clk_and_data(True, True)
